namespace BasecampSocial.Api.Models.Upload;

/// <summary>Response for POST /api/v1/upload/presign.</summary>
public sealed record PresignResponse(
    string UploadUrl,
    string FileKey,
    DateTimeOffset ExpiresAt);

/// <summary>Request body for POST /api/v1/upload/presign.</summary>
public sealed record PresignRequest(
    string FileName,
    string ContentType);
