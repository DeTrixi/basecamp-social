using BasecampSocial.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BasecampSocial.Api.Data;

/// <summary>
/// The EF Core database context for Basecamp Social, combining ASP.NET Core
/// Identity tables with our application-specific entities.
///
/// Design decisions:
/// - Extends <see cref="IdentityDbContext{TUser, TRole, TKey}"/> rather than plain
///   DbContext so that Identity automatically creates its tables (AspNetUsers,
///   AspNetRoles, AspNetUserClaims, etc.) alongside ours.
/// - Uses <c>Guid</c> for both the Identity key type and all entity primary keys
///   for consistency and PostgreSQL UUID compatibility.
/// - All entity configurations are defined in <see cref="OnModelCreating"/> using
///   the Fluent API rather than data annotations, because:
///   (1) It keeps entity classes clean (POCOs).
///   (2) It handles composite keys, which data annotations can't express.
///   (3) It centralises all DB schema decisions in one place.
/// - Enums are stored as strings (VARCHAR) rather than integers so that the
///   database is human-readable and adding new enum values doesn't break existing
///   data (no magic numbers).
/// - The critical index <c>(ConversationId, CreatedAt DESC)</c> on Messages enables
///   efficient cursor-based pagination for chat history.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Application DbSets
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<KeyBundle> KeyBundles => Set<KeyBundle>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReceipt> MessageReceipts => Set<MessageReceipt>();
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();
    public DbSet<PollVote> PollVotes => Set<PollVote>();
    public DbSet<DevicePushToken> DevicePushTokens => Set<DevicePushToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Let Identity configure its own tables first
        base.OnModelCreating(builder);

        // ── AppUser ──────────────────────────────────────────
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.DisplayName)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(u => u.AvatarUrl)
                  .HasMaxLength(2048);

            entity.Property(u => u.StatusMessage)
                  .HasMaxLength(200);

            entity.HasIndex(u => u.DisplayName);
        });

        // ── RefreshToken ─────────────────────────────────────
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);

            entity.Property(rt => rt.Token)
                  .HasMaxLength(256)
                  .IsRequired();

            // Unique index on Token for fast lookup during refresh
            entity.HasIndex(rt => rt.Token)
                  .IsUnique();

            // Index for finding active tokens by user
            entity.HasIndex(rt => rt.UserId);

            // Computed properties are NOT mapped to the database
            entity.Ignore(rt => rt.IsRevoked);
            entity.Ignore(rt => rt.IsExpired);
            entity.Ignore(rt => rt.IsActive);

            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── KeyBundle ────────────────────────────────────────
        builder.Entity<KeyBundle>(entity =>
        {
            entity.HasKey(kb => kb.Id);

            entity.Property(kb => kb.IdentityKey).IsRequired();
            entity.Property(kb => kb.SignedPreKey).IsRequired();
            entity.Property(kb => kb.SignedPreKeySignature).IsRequired();

            // Index for fetching a user's key bundle
            entity.HasIndex(kb => kb.UserId);

            entity.HasOne(kb => kb.User)
                  .WithMany(u => u.KeyBundles)
                  .HasForeignKey(kb => kb.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Conversation ─────────────────────────────────────
        builder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Type)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(c => c.Name)
                  .HasMaxLength(100);

            entity.Property(c => c.AvatarUrl)
                  .HasMaxLength(2048);

            entity.HasOne(c => c.Creator)
                  .WithMany()
                  .HasForeignKey(c => c.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ConversationMember ───────────────────────────────
        builder.Entity<ConversationMember>(entity =>
        {
            // Composite primary key: one membership per user per conversation
            entity.HasKey(cm => new { cm.ConversationId, cm.UserId });

            entity.Property(cm => cm.Role)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.HasOne(cm => cm.Conversation)
                  .WithMany(c => c.Members)
                  .HasForeignKey(cm => cm.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cm => cm.User)
                  .WithMany(u => u.ConversationMemberships)
                  .HasForeignKey(cm => cm.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Message ──────────────────────────────────────────
        builder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.EncryptedPayload)
                  .IsRequired();

            entity.Property(m => m.MessageType)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            // Critical index for cursor-based pagination of chat history:
            // "Give me the next 50 messages in conversation X before timestamp Y"
            entity.HasIndex(m => new { m.ConversationId, m.CreatedAt })
                  .IsDescending(false, true);

            entity.HasOne(m => m.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(m => m.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                  .WithMany(u => u.SentMessages)
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MessageReceipt ───────────────────────────────────
        builder.Entity<MessageReceipt>(entity =>
        {
            // Composite primary key: one receipt per user per message
            entity.HasKey(mr => new { mr.MessageId, mr.UserId });

            entity.HasOne(mr => mr.Message)
                  .WithMany(m => m.Receipts)
                  .HasForeignKey(mr => mr.MessageId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mr => mr.User)
                  .WithMany(u => u.MessageReceipts)
                  .HasForeignKey(mr => mr.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Poll ─────────────────────────────────────────────
        builder.Entity<Poll>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Title)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(p => p.Description)
                  .HasMaxLength(1000);

            entity.Property(p => p.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.HasIndex(p => p.ConversationId);

            entity.HasOne(p => p.Conversation)
                  .WithMany(c => c.Polls)
                  .HasForeignKey(p => p.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Creator)
                  .WithMany(u => u.CreatedPolls)
                  .HasForeignKey(p => p.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.ChosenOption)
                  .WithMany()
                  .HasForeignKey(p => p.ChosenOptionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── PollOption ───────────────────────────────────────
        builder.Entity<PollOption>(entity =>
        {
            entity.HasKey(po => po.Id);

            entity.Property(po => po.Label)
                  .HasMaxLength(100);

            entity.HasIndex(po => po.PollId);

            entity.HasOne(po => po.Poll)
                  .WithMany(p => p.Options)
                  .HasForeignKey(po => po.PollId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PollVote ─────────────────────────────────────────
        builder.Entity<PollVote>(entity =>
        {
            // Composite primary key: one vote per user per option
            entity.HasKey(pv => new { pv.PollOptionId, pv.UserId });

            entity.Property(pv => pv.Response)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .IsRequired();

            entity.HasOne(pv => pv.PollOption)
                  .WithMany(po => po.Votes)
                  .HasForeignKey(pv => pv.PollOptionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pv => pv.User)
                  .WithMany(u => u.PollVotes)
                  .HasForeignKey(pv => pv.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── DevicePushToken ──────────────────────────────────
        builder.Entity<DevicePushToken>(entity =>
        {
            entity.HasKey(dpt => dpt.Id);

            entity.Property(dpt => dpt.Token)
                  .HasMaxLength(512)
                  .IsRequired();

            entity.Property(dpt => dpt.Platform)
                  .HasMaxLength(10)
                  .IsRequired();

            // Unique index: one token string per device (prevents duplicates
            // if the same token is registered twice)
            entity.HasIndex(dpt => dpt.Token)
                  .IsUnique();

            entity.HasIndex(dpt => dpt.UserId);

            entity.HasOne(dpt => dpt.User)
                  .WithMany(u => u.DevicePushTokens)
                  .HasForeignKey(dpt => dpt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
