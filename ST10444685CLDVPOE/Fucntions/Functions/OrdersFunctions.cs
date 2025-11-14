using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Azure.Data.Tables;
using ABCRetailers.Models;

namespace ABCRetailers.Functions.Functions
{
    public class OrdersFunctions
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<OrdersFunctions> _logger;

        public OrdersFunctions(TableServiceClient tableServiceClient, ILogger<OrdersFunctions> logger)
        {
            _tableServiceClient = tableServiceClient;
            _logger = logger;
        }

        [Function("CreateOrder")]
        public async Task<HttpResponseData> CreateOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
        {
            try
            {
                // Log the incoming request
                _logger.LogInformation("📥 CreateOrder function triggered");
                Console.WriteLine("=== ORDER CREATION REQUEST RECEIVED ===");
                Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Request URL: {req.Url}");
                Console.WriteLine($"Request Method: {req.Method}");

                // Read and log the request body - FIXED THIS LINE
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Console.WriteLine($"Request Body: {requestBody}");

                // Reset the stream position so ReadFromJsonAsync can read it - ADDED THIS LINE
                req.Body.Position = 0;

                var order = await req.ReadFromJsonAsync<Order>();
                if (order == null)
                {
                    Console.WriteLine("❌ ERROR: Invalid order data - order is null");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { Success = false, Message = "Invalid order data" });
                    return badResponse;
                }

                // Log the order data
                Console.WriteLine($"Order Data Received:");
                Console.WriteLine($"  Customer ID: {order.CustomerId}");
                Console.WriteLine($"  Username: {order.Username}");
                Console.WriteLine($"  Product ID: {order.ProductId}");
                Console.WriteLine($"  Product Name: {order.ProductName}");
                Console.WriteLine($"  Order Date: {order.OrderDate}");
                Console.WriteLine($"  Quantity: {order.Quantity}");
                Console.WriteLine($"  Unit Price: {order.UnitPrice:C}");
                Console.WriteLine($"  Total Price: {order.TotalPrice:C}");
                Console.WriteLine($"  Status: {order.Status}");

                var tableClient = _tableServiceClient.GetTableClient("Orders");
                await tableClient.CreateIfNotExistsAsync();

                // Ensure keys are set (in case they're not provided)
                if (string.IsNullOrEmpty(order.RowKey))
                {
                    order.RowKey = Guid.NewGuid().ToString();
                    Console.WriteLine($"Generated RowKey: {order.RowKey}");
                }

                if (string.IsNullOrEmpty(order.PartitionKey))
                {
                    order.PartitionKey = "Orders";
                }

                Console.WriteLine($"Saving to Azure Table Storage...");
                Console.WriteLine($"  Table: Orders");
                Console.WriteLine($"  PartitionKey: {order.PartitionKey}");
                Console.WriteLine($"  RowKey: {order.RowKey}");
                Console.WriteLine($"  Order ID: {order.OrderId}");

                await tableClient.AddEntityAsync(order);

                // Log success
                _logger.LogInformation($"✅ Order created successfully: {order.OrderId} for {order.Username}");
                Console.WriteLine($"✅ ORDER CREATED SUCCESSFULLY:");
                Console.WriteLine($"   Order ID: {order.OrderId}");
                Console.WriteLine($"   RowKey: {order.RowKey}");
                Console.WriteLine($"   Customer ID: {order.CustomerId}");
                Console.WriteLine($"   Username: {order.Username}");
                Console.WriteLine($"   Product ID: {order.ProductId}");
                Console.WriteLine($"   Product Name: {order.ProductName}");
                Console.WriteLine($"   Order Date: {order.OrderDate}");
                Console.WriteLine($"   Quantity: {order.Quantity}");
                Console.WriteLine($"   Unit Price: {order.UnitPrice:C}");
                Console.WriteLine($"   Total Price: {order.TotalPrice:C}");
                Console.WriteLine($"   Status: {order.Status}");
                Console.WriteLine($"   PartitionKey: {order.PartitionKey}");
                Console.WriteLine("=========================================");

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(new { Success = true, Data = order, Message = "Order created successfully" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                Console.WriteLine($"❌ ERROR CREATING ORDER: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("=========================================");

                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new { Success = false, Message = $"Error: {ex.Message}" });
                return response;
            }
        }

        // KEEP THE REST OF YOUR CODE EXACTLY THE SAME
        [Function("GetOrders")]
        public async Task<HttpResponseData> GetOrders(
     [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequestData req)
        {
            try
            {
                // NO LOGGING AT ALL
                var tableClient = _tableServiceClient.GetTableClient("Orders");
                await tableClient.CreateIfNotExistsAsync();

                var orders = new List<Order>();
                await foreach (var order in tableClient.QueryAsync<Order>())
                {
                    orders.Add(order);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Success = true, Data = orders, Message = "Orders retrieved successfully" });
                return response;
            }
            catch (Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new { Success = false, Message = $"Error: {ex.Message}" });
                return response;
            }
        }

        [Function("DeleteOrder")]
        public async Task<HttpResponseData> DeleteOrder(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "orders/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("🗑️ DeleteOrder function triggered");
                Console.WriteLine("=== ORDER DELETION REQUEST RECEIVED ===");
                Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Order ID to delete: {id}");

                var tableClient = _tableServiceClient.GetTableClient("Orders");

                // Check if order exists
                try
                {
                    var order = await tableClient.GetEntityAsync<Order>("Orders", id);
                    Console.WriteLine($"Order found: {order.Value.OrderId} for {order.Value.Username}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Order not found with ID: {id}");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteAsJsonAsync(new { Success = false, Message = $"Order with ID {id} not found" });
                    return notFoundResponse;
                }

                Console.WriteLine($"Deleting order from Azure Table Storage...");
                await tableClient.DeleteEntityAsync("Orders", id);

                _logger.LogInformation($"✅ Order deleted successfully: {id}");
                Console.WriteLine($"✅ ORDER DELETED SUCCESSFULLY:");
                Console.WriteLine($"   Deleted Order ID: {id}");
                Console.WriteLine("=========================================");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Success = true, Message = "Order deleted successfully" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order");
                Console.WriteLine($"❌ ERROR DELETING ORDER: {ex.Message}");
                Console.WriteLine("=========================================");

                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new { Success = false, Message = $"Error: {ex.Message}" });
                return response;
            }
        }
    }
}