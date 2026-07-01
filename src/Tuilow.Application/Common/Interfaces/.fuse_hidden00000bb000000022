namespace DogMaster.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder, CancellationToken ct = default);
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
    string GetSignedUrl(string fileKey, int expirationMinutes = 60);
}
