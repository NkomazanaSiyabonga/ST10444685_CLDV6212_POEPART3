using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<CartController> _logger;

        public CartController(IFunctionsApi functionsApi, ILogger<CartController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // GET: /Cart - View shopping cart
        public async Task<IActionResult> Index()
        {
            var cartItems = GetSessionCart();
            var cartViewModel = await CreateCartViewModelAsync(cartItems);
            return View(cartViewModel);
        }

        // POST: /Cart/AddToCart - Add product to cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            try
            {
                // Get product details
                var product = await _functionsApi.GetProductAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (quantity > product.StockAvailable)
                {
                    return Json(new { success = false, message = "Not enough stock available" });
                }

                // Get current cart
                var cartItems = GetSessionCart();

                // Check if item already exists in cart
                var existingItem = cartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    var newItem = new CartItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = productId,
                        ProductName = product.ProductName,
                        UnitPrice = product.Price,
                        Quantity = quantity
                    };
                    cartItems.Add(newItem);
                }

                // Save cart back to session
                SaveSessionCart(cartItems);

                _logger.LogInformation("Product {ProductName} added to cart. Quantity: {Quantity}", product.ProductName, quantity);

                return Json(new
                {
                    success = true,
                    message = $"{product.ProductName} added to cart successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return Json(new { success = false, message = "Error adding product to cart" });
            }
        }

        // POST: /Cart/UpdateQuantity - Update item quantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(string productId, int quantity)
        {
            try
            {
                var cartItems = GetSessionCart();
                var cartItem = cartItems.FirstOrDefault(ci => ci.ProductId == productId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Item not found in cart" });
                }

                // Check stock availability
                var product = await _functionsApi.GetProductAsync(productId);
                if (product == null || quantity > product.StockAvailable)
                {
                    return Json(new { success = false, message = "Not enough stock available" });
                }

                cartItem.Quantity = quantity;
                SaveSessionCart(cartItems);

                var cartViewModel = await CreateCartViewModelAsync(cartItems);
                return Json(new
                {
                    success = true,
                    totalPrice = cartItem.TotalPrice,
                    grandTotal = cartViewModel.GrandTotal,
                    totalItems = cartViewModel.TotalItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                return Json(new { success = false, message = "Error updating quantity" });
            }
        }

        // POST: /Cart/RemoveItem - Remove item from cart
        [HttpPost]
        public async Task<IActionResult> RemoveItem(string productId)
        {
            try
            {
                var cartItems = GetSessionCart();
                var cartItem = cartItems.FirstOrDefault(ci => ci.ProductId == productId);

                if (cartItem != null)
                {
                    cartItems.Remove(cartItem);
                    SaveSessionCart(cartItems);
                }

                var cartViewModel = await CreateCartViewModelAsync(cartItems);
                return Json(new
                {
                    success = true,
                    grandTotal = cartViewModel.GrandTotal,
                    totalItems = cartViewModel.TotalItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new { success = false, message = "Error removing item" });
            }
        }

        // POST: /Cart/ClearCart - Clear all items from cart
        [HttpPost]
        public IActionResult ClearCart()
        {
            try
            {
                SaveSessionCart(new List<CartItem>());
                TempData["Success"] = "Cart cleared successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "Error clearing cart";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Cart/Confirmation - Order confirmation
        public IActionResult Confirmation()
        {
            return View();
        }

        // Session-based cart methods
        private List<CartItem> GetSessionCart()
        {
            var cartJson = HttpContext.Session.GetString("ShoppingCart");
            if (!string.IsNullOrEmpty(cartJson))
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            return new List<CartItem>();
        }

        private void SaveSessionCart(List<CartItem> cartItems)
        {
            var cartJson = JsonSerializer.Serialize(cartItems);
            HttpContext.Session.SetString("ShoppingCart", cartJson);
        }

        private async Task<CartViewModel> CreateCartViewModelAsync(List<CartItem> cartItems)
        {
            var viewModel = new CartViewModel
            {
                CartId = "session-cart",
                CustomerId = User.FindFirst("CustomerId")?.Value ?? "unknown",
                CustomerName = User.Identity?.Name ?? "Customer"
            };

            foreach (var item in cartItems)
            {
                var product = await _functionsApi.GetProductAsync(item.ProductId);
                viewModel.Items.Add(new CartItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Price = item.UnitPrice,
                    Quantity = item.Quantity,
                    StockAvailable = product?.StockAvailable ?? 0,
                    ImageUrl = product?.ProductImageUrl
                });
            }

            return viewModel;
        }
    }
}