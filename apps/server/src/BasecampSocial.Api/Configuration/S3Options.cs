namespace BasecampSocial.Api.Configuration;

/// <summary>
/// Strongly-typed options for S3-compatible object storage (MinIO in dev, AWS S3
/// or Cloudflare R2 in production), bound from the "S3" section of appsettings.json.
///
/// The app uses pre-signed URLs for uploads: the client requests a signed URL from
/// the API, encrypts the file locally, then uploads directly to S3. This keeps
/// large file transfers off the API server and preserves E2EE (the server never
/// sees the unencrypted file).
/// </summary>
public class S3Options
{
    public const string SectionName = "S3";

    /// <summary>
    /// The S3 endpoint URL. For MinIO: "http://localhost:9000".
    /// For AWS S3: leave null/empty to use the default AWS endpoint.
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>S3 access key (MinIO root user in dev).</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>S3 secret key (MinIO root password in dev).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>The bucket where encrypted media files are stored.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Use path-style addressing (e.g. http://localhost:9000/bucket/key) instead of
    /// virtual-hosted style (e.g. http://bucket.s3.amazonaws.com/key). Required for
    /// MinIO and most self-hosted S3-compatible stores.
    /// </summary>
    public bool UsePathStyle { get; set; } = true;
}
