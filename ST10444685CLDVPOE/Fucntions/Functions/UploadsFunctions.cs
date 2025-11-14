using ABCRetailers.Functions.Models;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ABCRetailers.Functions.Functions
{
    public class UploadsFunctions
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<UploadsFunctions> _logger;

        public UploadsFunctions(BlobServiceClient blobServiceClient, ILogger<UploadsFunctions> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        [Function("UploadFile")]
        public async Task<HttpResponseData> UploadFile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload")] HttpRequestData req)
        {
            try
            {
                var uploadDto = await req.ReadFromJsonAsync<FileUploadDto>();
                if (uploadDto == null || string.IsNullOrEmpty(uploadDto.FileData))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid file data"
                    });
                    return badResponse;
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(uploadDto.ContainerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobName = $"{Guid.NewGuid()}_{uploadDto.FileName}";
                var blobClient = containerClient.GetBlobClient(blobName);

                var fileBytes = Convert.FromBase64String(uploadDto.FileData);
                using var stream = new MemoryStream(fileBytes);

                await blobClient.UploadAsync(stream, true);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = true,
                    Data = blobClient.Uri.ToString(),
                    Message = "File uploaded successfully"
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
                return response;
            }
        }
    }
}