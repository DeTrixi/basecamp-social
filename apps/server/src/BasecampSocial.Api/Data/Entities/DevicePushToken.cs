namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// Stores an Expo Push Notification token for a user's device, enabling the server
/// to send push notifications when the user is offline.
/// 
/// Design decisions:
/// - A user can have multiple devices (phone + tablet), so this is a one-to-many
///   relationship from <see cref="AppUser"/>. When sending a push notification,
///   the server sends to ALL of the user's registered tokens.
/// - <see cref="Token"/> is the Expo push token (e.g. "ExponentPushToken[xxxxxx]").
///   This is an opaque string provided by Expo's push notification service, which
///   wraps APNs (iOS) and FCM (Android) behind a unified API.
/// - <see cref="Platform"/> tracks whether the device is iOS or Android. This is
///   useful for platform-specific notification formatting and debugging delivery
///   issues.
/// - The push notification payload sent by the server contains NO message content —
///   only a signal that a new message is available. The client then fetches and
///   decrypts the message locally. This preserves the zero-knowledge E2EE guarantee.
/// - Tokens can become stale (e.g. app uninstalled, token rotation). The server
///   should remove tokens that receive error responses from the Expo push service.
///   Stale token cleanup is handled in the notification service.
/// </summary>
public class DevicePushToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Expo push token string (e.g. "ExponentPushToken[xxxxxx]").</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Device platform: "ios" or "android". Used for platform-specific formatting.</summary>
    public string Platform { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public AppUser User { get; set; } = null!;
}
