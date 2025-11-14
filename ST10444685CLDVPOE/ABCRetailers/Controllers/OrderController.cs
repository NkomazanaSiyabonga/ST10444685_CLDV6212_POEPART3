using System.Security.Claims;
using System.Threading.Tasks;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IFunctionsApi api, ILogger<OrderController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // GET: /Order
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _api.GetOrdersAsync();
            return View(orders);
        }

        // GET: /Order/Create
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Create()
        {
            var customers = await _api.GetCustomersAsync();
            var products = await _api.GetProductsAsync();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products,
                OrderDate = DateTime.UtcNow
            };

            return View(viewModel);
        }

        // POST: /Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Create(OrderCreateViewModel orderModel)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdown data
                orderModel.Customers = await _api.GetCustomersAsync();
                orderModel.Products = await _api.GetProductsAsync();
                return View(orderModel);
            }

            try
            {
                // Get product details to set unit price
                var product = await _api.GetProductAsync(orderModel.ProductId);
                if (product != null)
                {
                    orderModel.UnitPrice = product.Price;
                    orderModel.ProductName = product.ProductName;
                }

                var success = await _api.CreateOrderAsync(orderModel);
                if (success)
                {
                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to create order. Please try again.");
                    orderModel.Customers = await _api.GetCustomersAsync();
                    orderModel.Products = await _api.GetProductsAsync();
                    return View(orderModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                orderModel.Customers = await _api.GetCustomersAsync();
                orderModel.Products = await _api.GetProductsAsync();
                return View(orderModel);
            }
        }

        // GET: /Order/Edit/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            var order = await _api.GetOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: /Order/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Order order)
        {
            if (order == null || string.IsNullOrEmpty(order.RowKey))
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                // Reload the original order so the view has the full data to render
                var originalForInvalid = await _api.GetOrderAsync(order.RowKey);
                if (originalForInvalid != null)
                    return View(originalForInvalid);

                return View(order);
            }

            try
            {
                // Load existing order from storage (preserve everything except the fields we want to change)
                var existingOrder = await _api.GetOrderAsync(order.RowKey);
                if (existingOrder == null)
                {
                    return NotFound();
                }

                // Update only the fields that should be editable from the Edit page.
                // In your view you allow editing Status and OrderDate — keep it limited.
                existingOrder.Status = order.Status;
                existingOrder.OrderDate = order.OrderDate;

                // If you ever add more editable fields, set them explicitly here.
                // Do NOT replace the whole entity (don't overwrite OrderItemsJson or CustomerId).

                var success = await _api.UpdateOrderAsync(existingOrder);
                if (success)
                {
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update order. Please try again.");
                    // Return the existing order so the form shows the full preserved values
                    return View(existingOrder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                ModelState.AddModelError("", $"Error updating order: {ex.Message}");

                // Return the original full order so nothing disappears in the view
                var original = await _api.GetOrderAsync(order.RowKey);
                return View(original ?? order);
            }
        }


        // POST: /Order/Delete/{id}
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var success = await _api.DeleteOrderAsync(id);
                if (success)
                {
                    TempData["Success"] = "Order deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete order.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Order/UpdateOrderStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                _logger.LogInformation("=== UPDATE ORDER STATUS STARTED ===");
                _logger.LogInformation("Order ID: {OrderId}, New Status: {NewStatus}", request.Id, request.NewStatus);

                // Validate input
                if (string.IsNullOrEmpty(request.Id))
                {
                    _logger.LogWarning("Order ID is null or empty");
                    return Json(new { success = false, message = "Order ID is required." });
                }

                if (string.IsNullOrEmpty(request.NewStatus))
                {
                    _logger.LogWarning("New status is null or empty");
                    return Json(new { success = false, message = "New status is required." });
                }

                // Use the status-only update method to avoid overwriting order data
                _logger.LogInformation("Calling UpdateOrderStatusAsync...");
                var success = await _api.UpdateOrderStatusAsync(request.Id, request.NewStatus);

                if (success)
                {
                    _logger.LogInformation("=== UPDATE SUCCESS ===");
                    _logger.LogInformation("Order {OrderId} status updated to {NewStatus}", request.Id, request.NewStatus);
                    return Json(new { success = true, message = "Order status updated successfully!" });
                }
                else
                {
                    _logger.LogError("=== UPDATE FAILED ===");
                    _logger.LogError("Failed to update order status for: {OrderId}", request.Id);
                    return Json(new { success = false, message = "Failed to update order status." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== UPDATE ERROR ===");
                _logger.LogError("Error updating order status for order: {OrderId}", request.Id);
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

      

        public class UpdateOrderStatusRequest
        {
            public string Id { get; set; } = string.Empty;
            public string NewStatus { get; set; } = string.Empty;
        }

        // GET: /Order/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var order = await _api.GetOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // GET: /Order/MyOrders
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyOrders()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    TempData["Error"] = "User not found. Please log in again.";
                    return RedirectToAction("CustomerDashboard", "Home");
                }

                // Get orders for the current customer
                var allOrders = await _api.GetOrdersAsync() ?? new List<Order>();
                var customerOrders = allOrders
                    .Where(o => o.Username == username)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                ViewBag.UserEmail = username;
                return View(customerOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load orders for user: {Username}", User.Identity?.Name);
                TempData["Error"] = "Could not load your orders. Please try again.";
                return View(new List<Order>());
            }
        }

        // GET: /Order/OrderDetailsPartial/{id}
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> OrderDetailsPartial(string id)
        {
            try
            {
                var order = await _api.GetOrderAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                // Verify the order belongs to the current user
                if (order.Username != User.Identity?.Name)
                {
                    return Forbid();
                }

                return PartialView("_OrderDetailsPartial", order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load order details for order: {OrderId}", id);
                return PartialView("_ErrorPartial", "Could not load order details.");
            }
        }

        // POST: /Order/CancelOrder
        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderRequest request)
        {
            try
            {
                var order = await _api.GetOrderAsync(request.Id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                // Verify the order belongs to the current user
                if (order.Username != User.Identity?.Name)
                {
                    return Json(new { success = false, message = "Access denied." });
                }

                // Only allow cancellation for certain statuses
                if (order.Status != "Submitted" && order.Status != "Processing")
                {
                    return Json(new { success = false, message = "This order cannot be cancelled." });
                }

                order.Status = "Cancelled";
                await _api.UpdateOrderAsync(order);

                _logger.LogInformation("Order {OrderId} cancelled by user {Username}", order.OrderId, User.Identity?.Name);

                return Json(new { success = true, message = "Order cancelled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order: {OrderId}", request.Id);
                return Json(new { success = false, message = "An error occurred while cancelling the order." });
            }
        }

        // POST: /Order/CreateFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateFromCart()
        {
            try
            {
                var customerId = User.FindFirst("CustomerId")?.Value;
                var username = User.Identity?.Name;

                _logger.LogInformation("Checkout started for user: {Username}, CustomerId: {CustomerId}", username, customerId);

                if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(username))
                {
                    TempData["Error"] = "Please log in to checkout.";
                    return RedirectToAction("Index", "Cart");
                }

                // Get customer details - try multiple methods
                Customer? customer = null;

                // Method 1: Try by CustomerId first
                if (!string.IsNullOrEmpty(customerId))
                {
                    customer = await _api.GetCustomerAsync(customerId);
                    _logger.LogInformation("Customer lookup by ID {CustomerId}: {Found}", customerId, customer != null);
                }

                // Method 2: Try by username if not found
                if (customer == null && !string.IsNullOrEmpty(username))
                {
                    customer = await _api.GetCustomerByUsernameAsync(username);
                    _logger.LogInformation("Customer lookup by username {Username}: {Found}", username, customer != null);
                }

                // Method 3: Create a temporary customer if still not found
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found in database. Creating temporary customer record.");

                    // Create a basic customer record
                    customer = new Customer
                    {
                        RowKey = customerId ?? Guid.NewGuid().ToString(),
                        PartitionKey = "CUSTOMER",
                        Username = username,
                        Name = User.FindFirst(ClaimTypes.GivenName)?.Value ?? "Customer",
                        Surname = User.FindFirst(ClaimTypes.Surname)?.Value ?? "User",
                        Email = User.FindFirst(ClaimTypes.Email)?.Value ?? $"{username}@abcretailers.com",
                        ShippingAddress = "Address not provided"
                    };

                    // Try to save the customer
                    var customerCreated = await _api.CreateCustomerAsync(customer);
                    _logger.LogInformation("Temporary customer creation: {Success}", customerCreated);
                }

                if (customer == null)
                {
                    TempData["Error"] = "Customer information not found. Please update your profile.";
                    return RedirectToAction("Index", "Cart");
                }

                // Get cart from session
                var cartJson = HttpContext.Session.GetString("ShoppingCart");
                if (string.IsNullOrEmpty(cartJson))
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }

                var cartItems = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

                if (!cartItems.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }

                _logger.LogInformation("Processing checkout with {ItemCount} items for customer {CustomerName}",
                    cartItems.Count, customer.Username);

                // Convert cart items to order items
                var orderItems = new List<OrderItem>();
                foreach (var cartItem in cartItems)
                {
                    var product = await _api.GetProductAsync(cartItem.ProductId);
                    orderItems.Add(new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.ProductName,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice,
                        ImageUrl = product?.ProductImageUrl
                    });
                }

                // Create order from cart
                var orderModel = new OrderCreateViewModel
                {
                    CustomerId = customer.RowKey, // Use the customer's RowKey
                    Username = customer.Username,
                    OrderDate = DateTime.UtcNow,
                    ShippingAddress = customer.ShippingAddress,
                    CustomerEmail = customer.Email,
                    Status = "Submitted",
                    OrderItems = orderItems
                };

                var success = await _api.CreateOrderAsync(orderModel);
                if (success)
                {
                    // Clear the cart after successful checkout
                    HttpContext.Session.Remove("ShoppingCart");

                    _logger.LogInformation("Order created successfully for customer {CustomerId}", customer.RowKey);
                    TempData["Success"] = "Order created successfully! Your order is being processed.";
                    return RedirectToAction("Confirmation", "Cart");
                }
                else
                {
                    TempData["Error"] = "Failed to create order. Please try again.";
                    return RedirectToAction("Index", "Cart");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order from cart");
                TempData["Error"] = "Error during checkout. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // GET: /Order/DebugCustomerInfo
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DebugCustomerInfo()
        {
            var customerId = User.FindFirst("CustomerId")?.Value;
            var username = User.Identity?.Name;

            var debugInfo = new
            {
                Username = username,
                CustomerId = customerId,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };

            // Try to find customer
            Customer? customerById = null;
            Customer? customerByUsername = null;

            if (!string.IsNullOrEmpty(customerId))
            {
                customerById = await _api.GetCustomerAsync(customerId);
            }

            if (!string.IsNullOrEmpty(username))
            {
                customerByUsername = await _api.GetCustomerByUsernameAsync(username);
            }

            ViewBag.DebugInfo = debugInfo;
            ViewBag.CustomerById = customerById;
            ViewBag.CustomerByUsername = customerByUsername;

            return View();
        }
    }

    public class CancelOrderRequest
    {
        public string Id { get; set; } = string.Empty;
    }
}