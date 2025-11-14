using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Azure.Data.Tables;
using ABCRetailers.Models;
using System.Text.Json;
namespace ABCRetailers.Functions.Functions
{
    public class ProductsFunctions
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<ProductsFunctions> _logger;

        public ProductsFunctions(TableServiceClient tableServiceClient, ILogger<ProductsFunctions> logger)
        {
            _tableServiceClient = tableServiceClient;
            _logger = logger;
        }

        [Function("CreateProduct")]
public async Task<HttpResponseData> CreateProduct(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequestData req)
{
    try
    {
        // Log the incoming request
        _logger.LogInformation("📥 CreateProduct function triggered");
        Console.WriteLine("=== PRODUCT CREATION REQUEST RECEIVED ===");
        Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        // Read the request body as string first
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        Console.WriteLine($"Raw Request Body: {requestBody}");

        if (string.IsNullOrEmpty(requestBody))
        {
            Console.WriteLine("❌ ERROR: Request body is empty");
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { Success = false, Message = "Request body is empty" });
            return badResponse;
        }

        // Manually deserialize the JSON
        var product = JsonSerializer.Deserialize<Product>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (product == null)
        {
            Console.WriteLine("❌ ERROR: Failed to deserialize product data");
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { Success = false, Message = "Invalid product data" });
            return badResponse;
        }

        // Log the product data
        Console.WriteLine($"Product Data Received:");
        Console.WriteLine($"  Product Name: {product.ProductName}");
        Console.WriteLine($"  Description: {product.Description}");
        Console.WriteLine($"  Price: {product.Price:C}");
        Console.WriteLine($"  Stock Available: {product.StockAvailable}");
        Console.WriteLine($"  Product Image URL: {product.ProductImageUrl}");

        var tableClient = _tableServiceClient.GetTableClient("Products");
        await tableClient.CreateIfNotExistsAsync();

        // Ensure keys are set (in case they're not provided)
        if (string.IsNullOrEmpty(product.RowKey))
        {
            product.RowKey = Guid.NewGuid().ToString();
            Console.WriteLine($"Generated RowKey: {product.RowKey}");
        }

        if (string.IsNullOrEmpty(product.PartitionKey))
        {
            product.PartitionKey = "Products";
        }

        // Ensure ProductId is set
        if (string.IsNullOrEmpty(product.ProductId))
        {
            product.ProductId = Guid.NewGuid().ToString();
            Console.WriteLine($"Generated ProductId: {product.ProductId}");
        }

        Console.WriteLine($"Saving to Azure Table Storage...");
        Console.WriteLine($"  Table: Products");
        Console.WriteLine($"  PartitionKey: {product.PartitionKey}");
        Console.WriteLine($"  RowKey: {product.RowKey}");
        Console.WriteLine($"  Product ID: {product.ProductId}");

        await tableClient.AddEntityAsync(product);

        // Log success
        _logger.LogInformation($"✅ Product created successfully: {product.ProductName} (ID: {product.ProductId})");
        Console.WriteLine($"✅ PRODUCT CREATED SUCCESSFULLY:");
        Console.WriteLine($"   Product ID: {product.ProductId}");
        Console.WriteLine($"   RowKey: {product.RowKey}");
        Console.WriteLine($"   Name: {product.ProductName}");
        Console.WriteLine($"   Description: {product.Description}");
        Console.WriteLine($"   Price: {product.Price:C}");
        Console.WriteLine($"   Stock Available: {product.StockAvailable}");
        Console.WriteLine($"   Image URL: {product.ProductImageUrl}");
        Console.WriteLine($"   PartitionKey: {product.PartitionKey}");
        Console.WriteLine("=========================================");

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(new { Success = true, Data = product, Message = "Product created successfully" });
        return response;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating product");
        Console.WriteLine($"❌ ERROR CREATING PRODUCT: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        Console.WriteLine("=========================================");

        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        await response.WriteAsJsonAsync(new { Success = false, Message = $"Error: {ex.Message}" });
        return response;
    }
}

        [Function("GetProducts")]
        public async Task<HttpResponseData> GetProducts(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products")] HttpRequestData req)
        {
            try
            {
                // REMOVE ALL LOGGING - no Console.WriteLine, no _logger.LogInformation
                var tableClient = _tableServiceClient.GetTableClient("Products");
                await tableClient.CreateIfNotExistsAsync();

                var products = new List<Product>();
                await foreach (var product in tableClient.QueryAsync<Product>())
                {
                    products.Add(product);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Success = true, Data = products, Message = "Products retrieved successfully" });
                return response;
            }
            catch (Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new { Success = false, Message = $"Error: {ex.Message}" });
                return response;
            }
        }
        [Function("DeleteProduct")]
        public async Task<HttpResponseData> DeleteProduct(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("🗑️ DeleteProduct function triggered");
                Console.WriteLine("=== PRODUCT DELETION REQUEST RECEIVED ===");
                Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Product ID to delete: {id}");

                var tableClient = _tableServiceClient.GetTableClient("Products");

                // Check if product exists
                try
                {
                    var product = await tableClient.GetEntityAsync<Product>("Products", id);
                    Console.WriteLine($"Product found: {product.Value.ProductName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Product not found with ID: {id}");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteAsJsonAsync(new { Success = false, Message = $"Product with ID {id} not found" });
                    return notFoundResponse;
                }

                Console.WriteLine($"Deleting product from Azure Table Storage...");
                await tableClient.DeleteEntityAsync("Products", id);

                _logger.LogInformation($"✅ Product deleted successfully: {id}");
                Console.WriteLine($"✅ PRODUCT DELETED SUCCESSFULLY:");
                Console.WriteLine($"   Deleted Product ID: {id}");
                Console.WriteLine("=========================================");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Success = true, Message = "Product deleted successfully" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                Console.WriteLine($"❌ ERROR DELETING PRODUCT: {ex.Message}");
                Console.WriteLine("=========================================");

                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new { Success = false, Message = $"Error: {ex.Message}" });
                return response;
            }
        }
    }
}