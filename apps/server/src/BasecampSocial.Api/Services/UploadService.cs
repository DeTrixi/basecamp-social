using Amazon.S3;
using Amazon.S3.Model;
using BasecampSocial.Api.Configuration;
using BasecampSocial.Api.Models.Upload;
using Microsoft.Extensions.Options;

namespace BasecampSocial.Api.Services;

/// <summary>Generates pre-signed S3 URLs for file uploads.</summary>
public interface IUploadService
{
    Task<PresignResponse> GeneratePresignedUrlAsync(Guid userId, PresignRequest request);
}

public class UploadService : IUploadService
{
    private readonly IAmazonS3 _s3;
    private readonly S3Options _s3Options;

    public UploadService(IAmazonS3 s3, IOptions<S3Options> s3Options)
    {
        _s3 = s3;
        _s3Options = s3Options.Value;
    }

    public async Task<PresignResponse> GeneratePresignedUrlAsync(Guid userId, PresignRequest request)
    {
        var fileKey = $"uploads/{userId}/{Guid.NewGuid()}/{request.FileName}";
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var presignRequest = new GetPreSignedUrlRequest
        {
            BucketName = _s3Options.BucketName,
            Key = fileKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = request.ContentType
        };

        var url = await _s3.GetPreSignedURLAsync(presignRequest);

        return new PresignResponse(
            UploadUrl: url,
            FileKey: fileKey,
            ExpiresAt: new DateTimeOffset(expiresAt, TimeSpan.Zero));
    }
}
