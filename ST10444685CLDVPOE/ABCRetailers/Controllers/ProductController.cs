using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace ABCRetailers.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IFunctionsApi api, ILogger<ProductController> logger)
        {
            _api = api;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Index()
        {
            var products = await _api.GetProductsAsync();
            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return View(product);

            try
            {
                // Set the required Azure Table fields
                product.RowKey = Guid.NewGuid().ToString();
                product.PartitionKey = "PRODUCTS";

                _logger.LogInformation("Creating product: {ProductName}, Price: {Price}, Stock: {Stock}",
                    product.ProductName, product.Price, product.StockAvailable);

                var success = await _api.CreateProductAsync(product, imageFile);

                if (success)
                {
                    _logger.LogInformation("Product created successfully: {ProductName}", product.ProductName);
                    TempData["Success"] = $"Product '{product.ProductName}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("Product creation failed - API returned false for: {ProductName}", product.ProductName);
                    ModelState.AddModelError("", "Failed to create product. Please check Azure Functions are running.");
                    return View(product);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", product.ProductName);
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                return View(product);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string rowKey)
        {
            var product = await _api.GetProductAsync(rowKey);
            if (product == null)
                return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return View(product);

            try
            {
                var success = await _api.UpdateProductAsync(product.RowKey, product, imageFile);
                if (success)
                {
                    TempData["Success"] = $"Product '{product.ProductName}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to update product.");
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                return View(product);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string rowKey)
        {
            try
            {
                var success = await _api.DeleteProductAsync(rowKey);
                TempData[success ? "Success" : "Error"] =
                    success ? "Product deleted successfully!" : "Failed to delete product.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTestProduct()
        {
            try
            {
                var testProduct = new Product
                {
                    ProductName = "Test Product " + DateTime.Now.ToString("HHmmss"),
                    Description = "This is a test product",
                    Price = 19.99m,
                    StockAvailable = 10,
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "PRODUCTS"
                };

                var success = await _api.CreateProductAsync(testProduct, null);
                TempData[success ? "Success" : "Error"] =
                    success ? "Test product created successfully!" : "Test product creation failed.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Test product creation error: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestProductApi()
        {
            try
            {
                var testProduct = new Product
                {
                    ProductName = "API Test Product " + DateTime.Now.ToString("HHmmss"),
                    Description = "Testing API connection",
                    Price = 1.99m,
                    StockAvailable = 5,
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "PRODUCTS"
                };

                var success = await _api.CreateProductAsync(testProduct, null);
                TempData[success ? "Success" : "Error"] =
                    success ? "Product API test successful!" : "Product API test failed.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"API Test Exception: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}