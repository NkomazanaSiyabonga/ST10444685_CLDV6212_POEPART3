using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ABCRetailers.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CustomerController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(IFunctionsApi api, ILogger<CustomerController> logger)
        {
            _api = api;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Admin viewing customers");
                var customers = await _api.GetCustomersAsync();

                if (customers == null)
                {
                    _logger.LogWarning("GetCustomersAsync returned null");
                    customers = new List<Customer>();
                }

                _logger.LogInformation("Retrieved {Count} customers", customers.Count);
                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                TempData["Error"] = "Failed to load customers.";
                return View(new List<Customer>());
            }
        }

        // CREATE CUSTOMER - GET
        public IActionResult Create()
        {
            return View();
        }

        // CREATE CUSTOMER - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            try
            {
                _logger.LogInformation("Creating new customer: {Username}", customer.Username);

                // Set required fields
                customer.RowKey = Guid.NewGuid().ToString();
                customer.PartitionKey = "CUSTOMER";
                customer.CustomerId = customer.RowKey;

                var success = await _api.CreateCustomerAsync(customer);

                if (success)
                {
                    _logger.LogInformation("Customer created successfully: {Username}", customer.Username);
                    TempData["Success"] = $"Customer '{customer.Name} {customer.Surname}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to create customer. Please try again.");
                    return View(customer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer: {Username}", customer.Username);
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                return View(customer);
            }
        }

        // EDIT CUSTOMER - GET
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var customer = await _api.GetCustomerAsync(id);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer for edit: {Id}", id);
                TempData["Error"] = "Error loading customer details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // EDIT CUSTOMER - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            try
            {
                _logger.LogInformation("Updating customer: {Username} ({RowKey})", customer.Username, customer.RowKey);

                var success = await _api.UpdateCustomerAsync(customer);

                if (success)
                {
                    _logger.LogInformation("Customer updated successfully: {Username}", customer.Username);
                    TempData["Success"] = $"Customer '{customer.Name} {customer.Surname}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update customer. Please try again.");
                    return View(customer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {Username}", customer.Username);
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(customer);
            }
        }

        // DELETE CUSTOMER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid customer ID.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _logger.LogInformation("Deleting customer: {Id}", id);

                var success = await _api.DeleteCustomerAsync(id);

                if (success)
                {
                    _logger.LogInformation("Customer deleted successfully: {Id}", id);
                    TempData["Success"] = "Customer deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete customer. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {Id}", id);
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // CUSTOMER DETAILS
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var customer = await _api.GetCustomerAsync(id);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer details: {Id}", id);
                TempData["Error"] = "Error loading customer details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var customers = await _api.GetCustomersAsync();
                TempData["Success"] = $"API Connection Successful! Found {customers?.Count ?? 0} customers.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"API Connection Failed: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}