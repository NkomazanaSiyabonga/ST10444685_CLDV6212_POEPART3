using System.Text;
using System.Text.Json;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static ABCRetailers.Controllers.OrderController;

namespace ABCRetailers.Services
{
    public class FunctionsApiClient : IFunctionsApi
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public FunctionsApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["Functions:BaseUrl"] ?? "http://localhost:7217/api/";
        }

        // ===== CUSTOMER OPERATIONS =====
        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                Console.WriteLine($"=== GETTING CUSTOMERS FROM: {_baseUrl}customers ===");

                var response = await _httpClient.GetAsync($"{_baseUrl}customers");
                Console.WriteLine($"Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw Response: {content}");

                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<Customer>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Console.WriteLine($"API Success: {apiResponse?.Success}, Count: {apiResponse?.Data?.Count}");
                    return apiResponse?.Data ?? new List<Customer>();
                }
                else
                {
                    Console.WriteLine($"ERROR: Failed to get customers - {response.StatusCode}");
                    return new List<Customer>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetCustomersAsync: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return new List<Customer>();
            }
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}customers/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<Customer>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetCustomerAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<Customer?> GetCustomerByUsernameAsync(string username)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}customers/by-username/{username}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<Customer>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetCustomerByUsernameAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateCustomerAsync(Customer customer)
        {
            try
            {
                var customerDto = new
                {
                    name = customer.Name,
                    surname = customer.Surname,
                    username = customer.Username,
                    email = customer.Email,
                    shippingAddress = customer.ShippingAddress,
                    partitionKey = "CUSTOMER",
                    rowKey = customer.RowKey
                };

                var json = JsonSerializer.Serialize(customerDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"Sending customer to Functions: {json}");

                var response = await _httpClient.PostAsync($"{_baseUrl}customers", content);

                Console.WriteLine($"Functions response: {response.StatusCode}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CreateCustomerAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                var customerDto = new
                {
                    name = customer.Name,
                    surname = customer.Surname,
                    username = customer.Username,
                    email = customer.Email,
                    shippingAddress = customer.ShippingAddress,
                    partitionKey = "CUSTOMER",
                    rowKey = customer.RowKey
                };

                var json = JsonSerializer.Serialize(customerDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}customers/{customer.RowKey}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateCustomerAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}customers/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in DeleteCustomerAsync: {ex.Message}");
                return false;
            }
        }

        // ===== PRODUCT OPERATIONS =====
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}products");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<Product>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return apiResponse?.Data ?? new List<Product>();
                }
                return new List<Product>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetProductsAsync: {ex.Message}");
                return new List<Product>();
            }
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}products/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<Product>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetProductAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateProductAsync(Product product, IFormFile? imageFile)
        {
            try
            {
                Console.WriteLine("=== CREATE PRODUCT API CALL STARTED ===");

                string imageUrl = null;
                if (imageFile != null && imageFile.Length > 0)
                {
                    Console.WriteLine($"Processing image file: {imageFile.FileName}, Size: {imageFile.Length} bytes");
                    imageUrl = await UploadFileAsync(imageFile, "product-images");
                    Console.WriteLine($"Image uploaded successfully: {imageUrl}");
                }
                else
                {
                    Console.WriteLine("No image file provided");
                }

                // Create the product DTO with exact property names
                var productDto = new
                {
                    productName = product.ProductName,
                    description = product.Description,
                    price = product.Price,
                    stockAvailable = product.StockAvailable,
                    productImageUrl = imageUrl ?? string.Empty,
                    partitionKey = "PRODUCTS",
                    rowKey = product.RowKey ?? Guid.NewGuid().ToString()
                };

                var json = JsonSerializer.Serialize(productDto, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                Console.WriteLine($"Sending product data to Azure Function:");
                Console.WriteLine(json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var apiUrl = $"{_baseUrl}products";
                Console.WriteLine($"Calling Azure Function at: {apiUrl}");

                var response = await _httpClient.PostAsync(apiUrl, content);

                Console.WriteLine($"Azure Function Response Status: {(int)response.StatusCode} {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Azure Function Response Content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("=== AZURE FUNCTION ERROR ===");
                    Console.WriteLine($"Status Code: {response.StatusCode}");
                    Console.WriteLine($"Response: {responseContent}");

                    // Try to parse error message
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (errorResponse != null)
                        {
                            Console.WriteLine($"Error Message: {errorResponse.Message}");
                            Console.WriteLine($"Success Flag: {errorResponse.Success}");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Could not parse error response: {parseEx.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("=== AZURE FUNCTION SUCCESS ===");
                    // Try to parse success response
                    try
                    {
                        var successResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (successResponse != null)
                        {
                            Console.WriteLine($"Success Message: {successResponse.Message}");
                            Console.WriteLine($"Success Flag: {successResponse.Success}");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Could not parse success response: {parseEx.Message}");
                    }
                }

                Console.WriteLine("=== CREATE PRODUCT API CALL COMPLETED ===");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION IN CREATE PRODUCT ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(string id, Product product, IFormFile? imageFile)
        {
            try
            {
                string imageUrl = null;
                if (imageFile != null && imageFile.Length > 0)
                {
                    imageUrl = await UploadFileAsync(imageFile, "product-images");
                }

                var productDto = new
                {
                    productName = product.ProductName,
                    description = product.Description,
                    price = product.Price,
                    stockAvailable = product.StockAvailable,
                    productImageUrl = imageUrl ?? product.ProductImageUrl ?? string.Empty,
                    partitionKey = "PRODUCTS", // CHANGED FROM "Products" TO "PRODUCTS"
                    rowKey = id
                };

                var json = JsonSerializer.Serialize(productDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}products/{id}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateProductAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}products/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in DeleteProductAsync: {ex.Message}");
                return false;
            }
        }

        // ===== ORDER OPERATIONS =====
        public async Task<List<Order>> GetOrdersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}orders");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<Order>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return apiResponse?.Data ?? new List<Order>();
                }
                return new List<Order>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetOrdersAsync: {ex.Message}");
                return new List<Order>();
            }
        }

        public async Task<Order?> GetOrderAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}orders/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<Order>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetOrderAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateOrderAsync(OrderCreateViewModel orderModel)
        {
            try
            {
                // Get customer details
                var customer = await GetCustomerAsync(orderModel.CustomerId);
                if (customer == null)
                {
                    Console.WriteLine($"ERROR: Customer not found with ID: {orderModel.CustomerId}");
                    return false;
                }

                var orderDto = new
                {
                    customerId = orderModel.CustomerId,
                    username = customer.Username,
                    productId = orderModel.ProductId,
                    productName = orderModel.ProductName,
                    orderDate = orderModel.OrderDate.ToUniversalTime(),
                    quantity = orderModel.Quantity,
                    unitPrice = orderModel.UnitPrice,
                    totalPrice = orderModel.UnitPrice * orderModel.Quantity,
                    status = orderModel.Status ?? "Submitted",
                    partitionKey = "Orders",
                    rowKey = Guid.NewGuid().ToString()
                };

                var json = JsonSerializer.Serialize(orderDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"Sending order to Functions: {json}");

                var response = await _httpClient.PostAsync($"{_baseUrl}orders", content);

                Console.WriteLine($"Functions response: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CreateOrderAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            try
            {
                Console.WriteLine($"=== UPDATING ORDER: {order.RowKey} ===");
                Console.WriteLine($"CustomerId: {order.CustomerId}");
                Console.WriteLine($"Username: {order.Username}");
                Console.WriteLine($"OrderItems Count: {order.OrderItems?.Count}");
                Console.WriteLine($"OrderItemsJson: {order.OrderItemsJson}");

                // Create the order DTO with ALL required properties including OrderItemsJson
                var orderDto = new
                {
                    customerId = order.CustomerId,
                    username = order.Username,
                    orderDate = order.OrderDate,
                    totalAmount = order.TotalAmount,
                    status = order.Status,
                    orderItemsJson = order.OrderItemsJson, // CRITICAL: This preserves the order items
                    shippingAddress = order.ShippingAddress,
                    customerEmail = order.CustomerEmail,
                    partitionKey = order.PartitionKey,
                    rowKey = order.RowKey,

                    // Backward compatibility for single-item orders
                    productId = order.ProductId,
                    productName = order.ProductName,
                    quantity = order.Quantity,
                    unitPrice = order.UnitPrice,
                    totalPrice = order.TotalPrice
                };

                var json = JsonSerializer.Serialize(orderDto, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                Console.WriteLine($"Sending order update to Functions:");
                Console.WriteLine(json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}orders/{order.RowKey}", content);

                Console.WriteLine($"Update response: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response: {errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateOrderAsync: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> DeleteOrderAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}orders/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in DeleteOrderAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(string id, string newStatus)
        {
            try
            {
                // First get the current order to preserve all data
                var order = await GetOrderAsync(id);
                if (order == null)
                {
                    Console.WriteLine($"Order {id} not found for status update");
                    return false;
                }

                // Update only the status while preserving all other data
                order.Status = newStatus;

                // Use the full update method to preserve all order data
                return await UpdateOrderAsync(order);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateOrderStatusAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Order>> GetOrdersByCustomerIdAsync(string customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}orders/by-customer/{customerId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<Order>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return apiResponse?.Data ?? new List<Order>();
                }
                return new List<Order>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetOrdersByCustomerIdAsync: {ex.Message}");
                return new List<Order>();
            }
        }

        public async Task<bool> UpdateOrderStatusOnlyAsync(string id, string newStatus)
        {
            try
            {
                var statusDto = new { status = newStatus };
                var json = JsonSerializer.Serialize(statusDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Try different endpoint patterns
                var response = await _httpClient.PatchAsync($"{_baseUrl}orders/{id}", content);

                if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                {
                    // Fallback to PUT with minimal data
                    response = await _httpClient.PutAsync($"{_baseUrl}orders/{id}/status", content);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateOrderStatusOnlyAsync: {ex.Message}");
                return false;
            }
        }

        // ===== UPLOAD OPERATIONS =====
        public async Task<string?> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                var base64File = Convert.ToBase64String(fileBytes);

                var uploadDto = new
                {
                    fileName = file.FileName,
                    containerName = "proof-of-payments",
                    contentType = file.ContentType,
                    fileData = base64File,
                    orderId = orderId,
                    customerName = customerName
                };

                var json = JsonSerializer.Serialize(uploadDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}upload/proof-of-payment", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UploadProofOfPaymentAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> UploadFileAsync(IFormFile file, string containerName)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                var base64File = Convert.ToBase64String(fileBytes);

                var uploadDto = new
                {
                    fileName = file.FileName,
                    containerName = containerName,
                    contentType = file.ContentType,
                    fileData = base64File
                };

                var json = JsonSerializer.Serialize(uploadDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}upload", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UploadFileAsync: {ex.Message}");
                return null;
            }
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}