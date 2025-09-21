using ManualApp.Models;
using Npgsql;
using Dapper;

namespace ManualApp.Services
{
    public class ImageService
    {
        private readonly string _connectionString;

        public ImageService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<Image>> GetImagesByManualAsync(int manualId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Image>(
                "SELECT * FROM Images WHERE ManualId = @ManualId",
                new { ManualId = manualId }
            );
        }

        public async Task AddImageAsync(Image image)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "INSERT INTO Images (FilePath, ManualId) VALUES (@FilePath, @ManualId)",
                image
            );
        }
    }
}
