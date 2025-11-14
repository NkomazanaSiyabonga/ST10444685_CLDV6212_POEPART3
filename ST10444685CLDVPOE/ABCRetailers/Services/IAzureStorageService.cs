using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetailers.Services
{
    public interface IAzureStorageService
    {
        // Blob methods
        Task<string> UploadImageAsync(IFormFile file, string containerName);
        Task<string> UploadFileAsync(IFormFile file, string containerName);
        Task DeleteBlobAsync(string blobUrl, string containerName);

        // Table methods
        Task AddEntityAsync<T>(T entity) where T : class, ITableEntity, new();
        Task<IEnumerable<T>> GetEntitiesAsync<T>(string partitionKey = null) where T : class, ITableEntity, new();
        Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();
        Task UpdateEntityAsync<T>(T entity) where T : class, ITableEntity, new();
        Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();

        // Queue operations
        Task SendMessageAsync(string queueName, string message);
        Task<string?> ReceiveMessageAsync(string queueName);

        // File Share operations
        Task<string> UploadToFileShareAsync(IFormFile file, string shareName, string directoryName = "");
        Task<byte[]> DownloadFromFileShareAsync(string shareName, string fileName, string directoryName = "");
        Task<List<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new();
    }
}