using ABCRetailers.Models;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCRetailers.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly IConfiguration _config;

        public AzureStorageService(IConfiguration config)
        {
            _config = config;
            var connectionString = config.GetConnectionString("AzureStorage");

            _blobServiceClient = new BlobServiceClient(connectionString);
            _tableServiceClient = new TableServiceClient(connectionString);
            _queueServiceClient = new QueueServiceClient(connectionString);
        }

        // Blob methods
        public async Task<string> UploadImageAsync(IFormFile file, string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);

            return blobClient.Uri.ToString();
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            return await UploadImageAsync(file, containerName);
        }

        public async Task DeleteBlobAsync(string blobUrl, string containerName)
        {
            if (string.IsNullOrEmpty(blobUrl)) return;

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobName = Path.GetFileName(new Uri(blobUrl).LocalPath);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        // Table methods
        public async Task AddEntityAsync<T>(T entity) where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(entity);
        }

        public async Task<IEnumerable<T>> GetEntitiesAsync<T>(string partitionKey = null) where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            try
            {
                if (string.IsNullOrEmpty(partitionKey))
                {
                    var results = new List<T>();
                    await foreach (var entity in tableClient.QueryAsync<T>())
                    {
                        results.Add(entity);
                    }
                    return results;
                }
                else
                {
                    var results = new List<T>();
                    await foreach (var entity in tableClient.QueryAsync<T>(e => e.PartitionKey == partitionKey))
                    {
                        results.Add(entity);
                    }
                    return results;
                }
            }
            catch
            {
                return new List<T>();
            }
        }

        public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return null;

            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            try
            {
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateEntityAsync<T>(T entity) where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
        }

        public async Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return;

            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            try
            {
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
            }
            catch
            {
                // Table might not exist, which is fine
            }
        }

        // Queue operations
        public async Task SendMessageAsync(string queueName, string message)
        {
            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync();
                await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(message)));
            }
            catch
            {
                // Queue might not be available, but we shouldn't fail the main operation
            }
        }

        public async Task<string?> ReceiveMessageAsync(string queueName)
        {
            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);

                var messages = await queueClient.ReceiveMessagesAsync(1);
                if (messages.Value.Length > 0)
                {
                    var message = messages.Value[0];
                    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                    return Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Helper methods
        private string GetTableName<T>()
        {
            var type = typeof(T);

            if (type == typeof(Customer)) return "Customers";
            if (type == typeof(Product)) return "Products";
            if (type == typeof(Order)) return "Orders";

            return type.Name + "s";
        }

        public async Task<List<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new()
        {
            var entities = await GetEntitiesAsync<T>();
            return entities?.ToList() ?? new List<T>();
        }

        // File Share operations (stubs)
        public Task<string> UploadToFileShareAsync(IFormFile file, string shareName, string directoryName = "")
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> DownloadFromFileShareAsync(string shareName, string fileName, string directoryName = "")
        {
            throw new NotImplementedException();
        }
    }
}