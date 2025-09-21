using Amazon.S3;
using Amazon.S3.Transfer;

namespace ManualApp.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Service(IConfiguration config)
        {
            _s3Client = new AmazonS3Client(
                config["AWS:AccessKey"],
                config["AWS:SecretKey"],
                Amazon.RegionEndpoint.GetBySystemName(config["AWS:Region"])
            );
            _bucketName = config["AWS:BucketName"];
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = _bucketName,
                ContentType = GetContentType(fileName)
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            return $"https://{_bucketName}.s3.amazonaws.com/{fileName}";
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            var key = ExtractKeyFromUrl(fileUrl);
            await _s3Client.DeleteObjectAsync(_bucketName, key);
        }

        private string ExtractKeyFromUrl(string fileUrl)
        {
            var uri = new Uri(fileUrl);
            return uri.AbsolutePath.TrimStart('/');
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".tiff" or ".tif" => "image/tiff",
                _ => "application/octet-stream" // 不明なファイルタイプの場合
            };
        }
    }
}
