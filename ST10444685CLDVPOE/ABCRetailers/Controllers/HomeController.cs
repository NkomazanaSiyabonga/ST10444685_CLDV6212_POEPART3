using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IFunctionsApi api, ILogger<HomeController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // Main Home Page
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _api.GetProductsAsync() ?? new List<Product>();
                var vm = new HomeViewModel
                {
                    FeaturedProducts = products.Take(8).ToList(),
                    ProductCount = products.Count
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load products for Home page");
                TempData["Error"] = "Could not load products. Please try again later.";
                return View(new HomeViewModel());
            }
        }

        // ADMIN DASHBOARD
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
                var products = await _api.GetProductsAsync() ?? new List<Product>();
                var orders = await _api.GetOrdersAsync() ?? new List<Order>();

                var dashboardModel = new AdminDashboardViewModel
                {
                    CustomerCount = customers.Count,
                    ProductCount = products.Count,
                    OrderCount = orders.Count,
                    FeaturedProducts = products.Take(3).ToList()
                };

                return View(dashboardModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View(new AdminDashboardViewModel());
            }
        }

        // API for dashboard counts
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> GetDashboardCounts()
        {
            try
            {
                var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
                var products = await _api.GetProductsAsync() ?? new List<Product>();
                var orders = await _api.GetOrdersAsync() ?? new List<Order>();

                return Json(new
                {
                    success = true,
                    customerCount = customers.Count,
                    productCount = products.Count,
                    orderCount = orders.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard counts");
                return Json(new { success = false });
            }
        }

        // CUSTOMER DASHBOARD
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CustomerDashboard()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                ViewBag.UserEmail = userEmail;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Customer Dashboard data.");
                TempData["Error"] = "Could not load your dashboard. Please try again.";
                return View();
            }
        }

        public IActionResult Contact()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}