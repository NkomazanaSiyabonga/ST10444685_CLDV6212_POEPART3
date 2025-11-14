using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace ABCRetailers.Services
{
    public class LocalFunctionsApi : IFunctionsApi
    {
        private readonly ILogger<LocalFunctionsApi> _logger;
        private readonly string _dataFolderPath;
        private readonly string _productsFile;
        private readonly string _customersFile;
        private readonly string _ordersFile;

        private List<Customer> _customers;
        private List<Product> _products;
        private List<Order> _orders;
        private List<Cart> _carts;
        private List<CartItem> _cartItems;

        public LocalFunctionsApi(ILogger<LocalFunctionsApi> logger)
        {
            _logger = logger;

            // Set up file paths for data persistence
            _dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "AppData");
            _productsFile = Path.Combine(_dataFolderPath, "products.json");
            _customersFile = Path.Combine(_dataFolderPath, "customers.json");
            _ordersFile = Path.Combine(_dataFolderPath, "orders.json");

            // Ensure data directory exists
            if (!Directory.Exists(_dataFolderPath))
            {
                Directory.CreateDirectory(_dataFolderPath);
            }

            // Load data from files or initialize with sample data
            _products = LoadProducts();
            _customers = LoadCustomers();
            _orders = LoadOrders();
            _carts = new List<Cart>();
            _cartItems = new List<CartItem>();

            _logger.LogInformation("LocalFunctionsApi initialized with {ProductCount} products and {CustomerCount} customers",
                _products.Count, _customers.Count);
        }

        // ===== FILE PERSISTENCE METHODS =====
        private List<Product> LoadProducts()
        {
            try
            {
                if (File.Exists(_productsFile))
                {
                    var json = File.ReadAllText(_productsFile);
                    var products = JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
                    _logger.LogInformation("Loaded {Count} products from file", products.Count);
                    return products;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products from file");
            }

            // Return sample data if no file exists
            return new List<Product>
            {
                new Product
                {
                    ProductName = "Wireless Headphones",
                    Description = "High-quality wireless headphones with noise cancellation",
                    Price = 99.99m,
                    StockAvailable = 25,
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "PRODUCTS",
                    ProductImageUrl = ""
                },
                new Product
                {
                    ProductName = "Smartphone",
                    Description = "Latest smartphone with advanced features",
                    Price = 699.99m,
                    StockAvailable = 15,
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "PRODUCTS",
                    ProductImageUrl = ""
                }
            };
        }

        private List<Customer> LoadCustomers()
        {
            try
            {
                if (File.Exists(_customersFile))
                {
                    var json = File.ReadAllText(_customersFile);
                    var customers = JsonSerializer.Deserialize<List<Customer>>(json) ?? new List<Customer>();
                    _logger.LogInformation("Loaded {Count} customers from file", customers.Count);
                    return customers;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers from file");
            }

            // Return sample data if no file exists
            return new List<Customer>
            {
                new Customer
                {
                    Name = "John",
                    Surname = "Doe",
                    Username = "johndoe",
                    Email = "john.doe@email.com",
                    ShippingAddress = "123 Main Street, Cityville",
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "CUSTOMER"
                }
            };
        }

        private List<Order> LoadOrders()
        {
            try
            {
                if (File.Exists(_ordersFile))
                {
                    var json = File.ReadAllText(_ordersFile);
                    return JsonSerializer.Deserialize<List<Order>>(json) ?? new List<Order>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders from file");
            }

            return new List<Order>();
        }

        private void SaveProducts()
        {
            try
            {
                var json = JsonSerializer.Serialize(_products, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_productsFile, json);
                _logger.LogInformation("Saved {Count} products to file", _products.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving products to file");
            }
        }

        private void SaveCustomers()
        {
            try
            {
                var json = JsonSerializer.Serialize(_customers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_customersFile, json);
                _logger.LogInformation("Saved {Count} customers to file", _customers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving customers to file");
            }
        }

        private void SaveOrders()
        {
            try
            {
                var json = JsonSerializer.Serialize(_orders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_ordersFile, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving orders to file");
            }
        }

        public Task<bool> UpdateOrderAsync(Order order)
        {
            try
            {
                _logger.LogInformation("Updating order: {OrderId}", order.RowKey);

                var existingOrder = _orders.FirstOrDefault(o => o.RowKey == order.RowKey);
                if (existingOrder != null)
                {
                    // Preserve the existing OrderItemsJson if the incoming order doesn't have it
                    if (string.IsNullOrEmpty(order.OrderItemsJson) && !string.IsNullOrEmpty(existingOrder.OrderItemsJson))
                    {
                        order.OrderItemsJson = existingOrder.OrderItemsJson;
                        _logger.LogInformation("Preserved OrderItemsJson from existing order");
                    }

                    // Log the order data being saved
                    _logger.LogInformation("Order data being saved - CustomerId: {CustomerId}, ProductId: {ProductId}, OrderItemsJson: {HasItemsJson}",
                        order.CustomerId, order.ProductId, !string.IsNullOrEmpty(order.OrderItemsJson));

                    _orders.Remove(existingOrder);
                    _orders.Add(order);
                    SaveOrders(); // Persist to file

                    _logger.LogInformation("Order updated successfully: {OrderId}", order.RowKey);
                    return Task.FromResult(true);
                }

                _logger.LogWarning("Order not found for update: {OrderId}", order.RowKey);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {RowKey}", order.RowKey);
                return Task.FromResult(false);
            }
        }

        // ===== CUSTOMER OPERATIONS =====
        public Task<List<Customer>> GetCustomersAsync()
        {
            _logger.LogInformation("Getting {Count} customers", _customers.Count);
            return Task.FromResult(_customers);
        }

        public Task<Customer?> GetCustomerAsync(string id)
        {
            var customer = _customers.FirstOrDefault(c => c.RowKey == id);
            return Task.FromResult(customer);
        }

        public Task<Customer?> GetCustomerByUsernameAsync(string username)
        {
            var customer = _customers.FirstOrDefault(c => c.Username == username);
            return Task.FromResult(customer);
        }

        public Task<bool> CreateCustomerAsync(Customer customer)
        {
            try
            {
                _logger.LogInformation("Creating customer: {Username}", customer.Username);

                // Set required fields if not set
                if (string.IsNullOrEmpty(customer.RowKey))
                    customer.RowKey = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(customer.PartitionKey))
                    customer.PartitionKey = "CUSTOMER";
                if (string.IsNullOrEmpty(customer.CustomerId))
                    customer.CustomerId = customer.RowKey;

                _customers.Add(customer);
                SaveCustomers(); // Persist to file

                _logger.LogInformation("Customer created successfully: {Username}", customer.Username);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer: {Username}", customer.Username);
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                var existingCustomer = _customers.FirstOrDefault(c => c.RowKey == customer.RowKey);
                if (existingCustomer != null)
                {
                    _customers.Remove(existingCustomer);
                    _customers.Add(customer);
                    SaveCustomers(); // Persist to file
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {RowKey}", customer.RowKey);
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeleteCustomerAsync(string id)
        {
            try
            {
                var customer = _customers.FirstOrDefault(c => c.RowKey == id);
                if (customer != null)
                {
                    _customers.Remove(customer);
                    SaveCustomers(); // Persist to file
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {Id}", id);
                return Task.FromResult(false);
            }
        }

        // ===== PRODUCT OPERATIONS =====
        public Task<List<Product>> GetProductsAsync()
        {
            _logger.LogInformation("Getting {Count} products", _products.Count);
            return Task.FromResult(_products);
        }

        public Task<Product?> GetProductAsync(string id)
        {
            var product = _products.FirstOrDefault(p => p.RowKey == id);
            return Task.FromResult(product);
        }

        public Task<bool> CreateProductAsync(Product product, IFormFile? imageFile)
        {
            try
            {
                _logger.LogInformation("Creating product: {ProductName}", product.ProductName);

                // Set required fields if not set
                if (string.IsNullOrEmpty(product.RowKey))
                    product.RowKey = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(product.PartitionKey))
                    product.PartitionKey = "PRODUCTS";

                // Handle image if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    product.ProductImageUrl = $"/images/placeholder-product.jpg";
                }

                _products.Add(product);
                SaveProducts(); // Persist to file

                _logger.LogInformation("Product created successfully: {ProductName}", product.ProductName);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", product.ProductName);
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateProductAsync(string id, Product product, IFormFile? imageFile)
        {
            try
            {
                var existingProduct = _products.FirstOrDefault(p => p.RowKey == id);
                if (existingProduct != null)
                {
                    _products.Remove(existingProduct);

                    // Handle image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        product.ProductImageUrl = $"/images/placeholder-product.jpg";
                    }

                    _products.Add(product);
                    SaveProducts(); // Persist to file
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {RowKey}", id);
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeleteProductAsync(string id)
        {
            try
            {
                var product = _products.FirstOrDefault(p => p.RowKey == id);
                if (product != null)
                {
                    _products.Remove(product);
                    SaveProducts(); // Persist to file
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {Id}", id);
                return Task.FromResult(false);
            }
        }

        // ===== ORDER OPERATIONS =====
        public Task<List<Order>> GetOrdersAsync() => Task.FromResult(_orders);

        public Task<Order?> GetOrderAsync(string id) => Task.FromResult(_orders.FirstOrDefault(o => o.RowKey == id));

        public Task<bool> CreateOrderAsync(OrderCreateViewModel orderModel)
        {
            try
            {
                var order = new Order
                {
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "ORDERS",
                    CustomerId = orderModel.CustomerId,
                    Username = orderModel.Username,
                    OrderDate = orderModel.OrderDate,
                    TotalAmount = orderModel.TotalAmount,
                    Status = orderModel.Status ?? "Submitted",
                    ShippingAddress = orderModel.ShippingAddress,
                    CustomerEmail = orderModel.CustomerEmail
                };

                // Handle both single product and cart-based orders
                if (orderModel.OrderItems.Any())
                {
                    order.OrderItems = orderModel.OrderItems;
                }
                else if (!string.IsNullOrEmpty(orderModel.ProductId))
                {
                    // Backward compatibility for single product orders
                    order.OrderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = orderModel.ProductId,
                    ProductName = orderModel.ProductName ?? "Unknown Product",
                    Quantity = orderModel.Quantity,
                    UnitPrice = orderModel.UnitPrice
                }
            };
                    order.TotalAmount = orderModel.UnitPrice * orderModel.Quantity;
                }

                _orders.Add(order);
                SaveOrders();

                _logger.LogInformation("Order created: {OrderId} for customer {CustomerId}", order.RowKey, order.CustomerId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return Task.FromResult(false);
            }
        }

       

        public Task<bool> DeleteOrderAsync(string id)
        {
            try
            {
                var order = _orders.FirstOrDefault(o => o.RowKey == id);
                if (order != null)
                {
                    _orders.Remove(order);
                    SaveOrders(); // Persist to file
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order: {Id}", id);
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateOrderStatusAsync(string id, string newStatus)
        {
            try
            {
                _logger.LogInformation("=== LOCAL API: UpdateOrderStatusAsync ===");
                _logger.LogInformation("Order: {OrderId}, New Status: {NewStatus}", id, newStatus);

                var order = _orders.FirstOrDefault(o => o.RowKey == id);
                if (order != null)
                {
                    _logger.LogInformation("Order found. Current status: {CurrentStatus}", order.Status);

                    // Only update the status, preserve all other data
                    order.Status = newStatus;
                    SaveOrders();

                    _logger.LogInformation("=== LOCAL API: UPDATE SUCCESS ===");
                    _logger.LogInformation("Order status updated from {OldStatus} to {NewStatus}", order.Status, newStatus);
                    return Task.FromResult(true);
                }

                _logger.LogWarning("=== LOCAL API: ORDER NOT FOUND ===");
                _logger.LogWarning("Order not found for status update: {OrderId}", id);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== LOCAL API: UPDATE ERROR ===");
                _logger.LogError("Error updating order status: {OrderId}", id);
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateOrderStatusOnlyAsync(string id, string newStatus)
        {
            try
            {
                _logger.LogInformation("UpdateOrderStatusOnlyAsync - Order: {OrderId}, New Status: {NewStatus}", id, newStatus);

                var order = _orders.FirstOrDefault(o => o.RowKey == id);
                if (order != null)
                {
                    // Only update the status, preserve all other data
                    order.Status = newStatus;
                    SaveOrders();

                    _logger.LogInformation("Order status updated successfully using UpdateOrderStatusOnlyAsync: {OrderId}", id);
                    return Task.FromResult(true);
                }

                _logger.LogWarning("Order not found for status update: {OrderId}", id);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateOrderStatusOnlyAsync for order: {OrderId}", id);
                return Task.FromResult(false);
            }
        }
        public Task<List<Order>> GetOrdersByCustomerIdAsync(string customerId) => Task.FromResult(new List<Order>());

        // ===== UPLOAD OPERATIONS =====
        public Task<string?> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName) => Task.FromResult("/images/placeholder.jpg");
        public Task<string?> UploadFileAsync(IFormFile file, string containerName) => Task.FromResult("/images/placeholder.jpg");

        // Cart simulation methods (session-based, not persisted)
        private Cart GetOrCreateCart(string customerId, string username)
        {
            var cart = _carts.FirstOrDefault(c => c.CustomerId == customerId);
            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerId,
                    CustomerUsername = username,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _carts.Add(cart);
            }
            return cart;
        }

        private List<CartItem> GetCartItems(string cartId)
        {
            return _cartItems.Where(ci => ci.CartId == cartId).ToList();
        }
    }
}