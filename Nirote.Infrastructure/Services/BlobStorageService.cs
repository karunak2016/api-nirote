using Amazon.S3;
using Amazon.S3.Model;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Nirote.Infrastructure.Services;

public class BlobStorageService
{
    private readonly string? _azureConnectionString;
    private readonly string  _containerName;

    private readonly string? _r2AccountId;
    private readonly string? _r2AccessKeyId;
    private readonly string? _r2SecretAccessKey;
    private readonly string? _r2BucketName;
    private readonly string? _r2PublicUrl;

    private readonly string? _localUploadsPath;
    private readonly string? _localBaseUrl;

    private BlobContainerClient? _azureClient;
    private AmazonS3Client?      _r2Client;

    private enum StorageBackend { Azure, R2, Local }
    private readonly StorageBackend _backend;

    public BlobStorageService(IConfiguration configuration, IWebHostEnvironment env)
    {
        _azureConnectionString = configuration["AzureStorage:ConnectionString"];
        _containerName         = configuration["AzureStorage:ContainerName"] ?? "products";

        _r2AccountId      = configuration["R2:AccountId"];
        _r2AccessKeyId    = configuration["R2:AccessKeyId"];
        _r2SecretAccessKey = configuration["R2:SecretAccessKey"];
        _r2BucketName     = configuration["R2:BucketName"] ?? "nirote-products";
        _r2PublicUrl      = configuration["R2:PublicUrl"]?.TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(_azureConnectionString))
        {
            _backend = StorageBackend.Azure;
        }
        else if (!string.IsNullOrWhiteSpace(_r2AccountId) &&
                 !string.IsNullOrWhiteSpace(_r2AccessKeyId) &&
                 !string.IsNullOrWhiteSpace(_r2SecretAccessKey))
        {
            _backend = StorageBackend.R2;
        }
        else
        {
            _backend = StorageBackend.Local;
            _localUploadsPath = Path.Combine(env.WebRootPath ?? env.ContentRootPath, "uploads");
            Directory.CreateDirectory(_localUploadsPath);
            _localBaseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5095";
        }
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType)
    {
        return _backend switch
        {
            StorageBackend.Azure => await UploadToAzureAsync(imageStream, fileName, contentType),
            StorageBackend.R2    => await UploadToR2Async(imageStream, fileName, contentType),
            _                    => await UploadToLocalAsync(imageStream, fileName),
        };
    }

    public async Task DeleteImageAsync(string imageUrl)
    {
        switch (_backend)
        {
            case StorageBackend.Azure:
                await DeleteFromAzureAsync(imageUrl);
                break;
            case StorageBackend.R2:
                await DeleteFromR2Async(imageUrl);
                break;
            default:
                DeleteFromLocal(imageUrl);
                break;
        }
    }

    // ── Azure ─────────────────────────────────────────────────────────────────

    private async Task<string> UploadToAzureAsync(Stream stream, string fileName, string contentType)
    {
        var container = await GetAzureContainerAsync();
        var blob = container.GetBlobClient(fileName);
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
        return blob.Uri.ToString();
    }

    private async Task DeleteFromAzureAsync(string imageUrl)
    {
        var container = await GetAzureContainerAsync();
        var blobName = Path.GetFileName(new Uri(imageUrl).LocalPath);
        await container.GetBlobClient(blobName).DeleteIfExistsAsync();
    }

    private async Task<BlobContainerClient> GetAzureContainerAsync()
    {
        if (_azureClient is null)
        {
            _azureClient = new BlobContainerClient(_azureConnectionString, _containerName);
            await _azureClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        }
        return _azureClient;
    }

    // ── Cloudflare R2 (S3-compatible) ────────────────────────────────────────

    private async Task<string> UploadToR2Async(Stream stream, string fileName, string contentType)
    {
        var client = GetR2Client();
        var request = new PutObjectRequest
        {
            BucketName          = _r2BucketName,
            Key                 = fileName,
            InputStream         = stream,
            ContentType         = contentType,
            DisablePayloadSigning = true,
        };
        await client.PutObjectAsync(request);

        // Return public URL — requires bucket public access enabled in R2 dashboard
        return string.IsNullOrWhiteSpace(_r2PublicUrl)
            ? $"https://{_r2AccountId}.r2.cloudflarestorage.com/{_r2BucketName}/{fileName}"
            : $"{_r2PublicUrl}/{fileName}";
    }

    private async Task DeleteFromR2Async(string imageUrl)
    {
        var client   = GetR2Client();
        var fileName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);
        await client.DeleteObjectAsync(_r2BucketName, fileName);
    }

    private AmazonS3Client GetR2Client()
    {
        if (_r2Client is null)
        {
            var config = new AmazonS3Config
            {
                ServiceURL    = $"https://{_r2AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
            };
            _r2Client = new AmazonS3Client(_r2AccessKeyId, _r2SecretAccessKey, config);
        }
        return _r2Client;
    }

    // ── Local fallback (development) ──────────────────────────────────────────

    private async Task<string> UploadToLocalAsync(Stream stream, string fileName)
    {
        var filePath = Path.Combine(_localUploadsPath!, fileName);
        using var fs = File.Create(filePath);
        await stream.CopyToAsync(fs);
        return $"{_localBaseUrl}/uploads/{fileName}";
    }

    private void DeleteFromLocal(string imageUrl)
    {
        var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
        var filePath = Path.Combine(_localUploadsPath!, fileName);
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}
