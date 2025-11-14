using System.Net;
using System.Text.Json;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Models;
using ABCRetailers.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Functions.Functions
{
    public class CustomersFunctions
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<CustomersFunctions> _logger;

        public CustomersFunctions(TableServiceClient tableServiceClient, ILogger<CustomersFunctions> logger)
        {
            _tableServiceClient = tableServiceClient;
            _logger = logger;
        }

        [Function("GetCustomers")]
        public async Task<HttpResponseData> GetCustomers(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers")] HttpRequestData req)
        {
            try
            {
                // NO LOGGING AT ALL
                var tableClient = _tableServiceClient.GetTableClient("Customers");
                await tableClient.CreateIfNotExistsAsync();

                var customers = new List<Customer>();
                await foreach (var customer in tableClient.QueryAsync<Customer>())
                {
                    customers.Add(customer);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Success = true, Data = customers, Message = "Customers retrieved successfully" });
                return response;
            }
            catch (Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new { Success = false, Message = $"Error: {ex.Message}" });
                return response;
            }
        }

        [Function("CreateCustomer")]
        public async Task<HttpResponseData> CreateCustomer(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers")] HttpRequestData req)
        {
            try
            {
                // Log the incoming request
                _logger.LogInformation("📥 CreateCustomer function triggered");
                Console.WriteLine("=== CUSTOMER CREATION REQUEST RECEIVED ===");
                Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Request URL: {req.Url}");
                Console.WriteLine($"Request Method: {req.Method}");

                // Read the request body as string first
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Console.WriteLine($"Request Body: {requestBody}");

                if (string.IsNullOrEmpty(requestBody))
                {
                    Console.WriteLine("❌ ERROR: Request body is empty");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { Success = false, Message = "Request body is empty" });
                    return badResponse;
                }

                // Manually deserialize the JSON instead of using ReadFromJsonAsync
                var customerDto = JsonSerializer.Deserialize<CustomerDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (customerDto == null)
                {
                    Console.WriteLine("❌ ERROR: Invalid customer data - customerDto is null");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { Success = false, Message = "Invalid customer data" });
                    return badResponse;
                }

                // Log the customer data
                Console.WriteLine($"Customer Data Received:");
                Console.WriteLine($"  Name: {customerDto.Name}");
                Console.WriteLine($"  Surname: {customerDto.Surname}");
                Console.WriteLine($"  Username: {customerDto.Username}");
                Console.WriteLine($"  Email: {customerDto.Email}");
                Console.WriteLine($"  Shipping Address: {customerDto.ShippingAddress}");
                Console.WriteLine($"  PartitionKey: {customerDto.PartitionKey}");
                Console.WriteLine($"  RowKey: {customerDto.RowKey}");

                var tableClient = _tableServiceClient.GetTableClient("Customers");
                await tableClient.CreateIfNotExistsAsync();

                var customerEntity = new CustomerEntity
                {
                    PartitionKey = customerDto.PartitionKey ?? "CUSTOMER",
                    RowKey = customerDto.RowKey ?? Guid.NewGuid().ToString(),
                    Name = customerDto.Name,
                    Surname = customerDto.Surname,
                    Username = customerDto.Username,
                    Email = customerDto.Email,
                    ShippingAddress = customerDto.ShippingAddress
                };

                Console.WriteLine($"Saving to Azure Table Storage...");
                Console.WriteLine($"  Table: Customers");
                Console.WriteLine($"  PartitionKey: {customerEntity.PartitionKey}");
                Console.WriteLine($"  RowKey: {customerEntity.RowKey}");

                await tableClient.AddEntityAsync(customerEntity);

                // Log success
                _logger.LogInformation($"✅ Customer created successfully: {customerEntity.Name} {customerEntity.Surname} (ID: {customerEntity.RowKey})");
                Console.WriteLine($"✅ CUSTOMER CREATED SUCCESSFULLY:");
                Console.WriteLine($"   ID: {customerEntity.RowKey}");
                Console.WriteLine($"   Name: {customerEntity.Name} {customerEntity.Surname}");
                Console.WriteLine($"   Username: {customerEntity.Username}");
                Console.WriteLine($"   Email: {customerEntity.Email}");
                Console.WriteLine($"   Shipping Address: {customerEntity.ShippingAddress}");
                Console.WriteLine($"   PartitionKey: {customerEntity.PartitionKey}");
                Console.WriteLine("=========================================");

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(new { Success = true, Data = customerEntity, Message = "Customer created successfully" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                Console.WriteLine($"❌ ERROR CREATING CUSTOMER: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("=========================================");

                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new { Success = false, Message = $"Error: {ex.Message}" });
                return response;
            }
        }

        [Function("GetCustomer")]
        public async Task<HttpResponseData> GetCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Customers");
                var customer = await tableClient.GetEntityAsync<CustomerEntity>("CUSTOMER", id);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponse<CustomerEntity>
                {
                    Success = true,
                    Data = customer.Value,
                    Message = "Customer retrieved successfully"
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer");
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