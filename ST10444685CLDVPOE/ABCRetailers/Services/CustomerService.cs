using ABCRetailers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetailers.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionsApi _functionsApi;

        public CustomerService(IAzureStorageService storageService, IFunctionsApi functionsApi)
        {
            _storageService = storageService;
            _functionsApi = functionsApi;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            try
            {
                Console.WriteLine("=== MVC SERVICE: Getting customers from Azure Storage ===");

                // Primary: Get from Azure Storage (MVC data source)
                var customers = await _storageService.GetEntitiesAsync<Customer>("Customers");

                // Fix: Use proper null handling and convert to list
                var customerList = customers?.ToList() ?? new List<Customer>();
                Console.WriteLine($"Retrieved {customerList.Count} customers from Azure Storage");

                // Optional: Sync with Functions API for reporting/analytics
                await TrySyncWithFunctionsApi(customerList);

                return customerList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetAllCustomersAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Customer?> GetCustomerAsync(string rowKey)
        {
            try
            {
                Console.WriteLine($"=== MVC SERVICE: Getting customer {rowKey} from Azure Storage ===");

                // Primary: Get from Azure Storage
                var customer = await _storageService.GetEntityAsync<Customer>("Customers", rowKey);
                return customer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetCustomerAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CreateCustomerAsync(Customer customer)
        {
            try
            {
                Console.WriteLine("=== MVC SERVICE: Creating customer in Azure Storage ===");

                // Step 1: Create in Azure Storage (primary data store)
                await _storageService.AddEntityAsync(customer);
                Console.WriteLine("SUCCESS: Customer created in Azure Storage");

                // Step 2: Sync with Functions API (secondary/async)
                await TryCreateInFunctionsApi(customer);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CreateCustomerAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                Console.WriteLine("=== MVC SERVICE: Updating customer in Azure Storage ===");

                // Step 1: Update in Azure Storage
                await _storageService.UpdateEntityAsync(customer);
                Console.WriteLine("SUCCESS: Customer updated in Azure Storage");

                // Step 2: Sync with Functions API
                await TryUpdateInFunctionsApi(customer);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateCustomerAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteCustomerAsync(string rowKey)
        {
            try
            {
                Console.WriteLine($"=== MVC SERVICE: Deleting customer {rowKey} from Azure Storage ===");

                // Step 1: Delete from Azure Storage
                await _storageService.DeleteEntityAsync<Customer>("Customers", rowKey);
                Console.WriteLine("SUCCESS: Customer deleted from Azure Storage");

                // Step 2: Sync with Functions API
                await TryDeleteFromFunctionsApi(rowKey);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in DeleteCustomerAsync: {ex.Message}");
                throw;
            }
        }

        // Helper methods for Functions API synchronization
        private async Task TrySyncWithFunctionsApi(List<Customer> customers)
        {
            try
            {
                // This could be used to ensure Functions API has the same data
                // For now, we'll just log that we're checking sync status
                var functionsCustomers = await _functionsApi.GetCustomersAsync();
                var functionsCustomerCount = functionsCustomers?.Count ?? 0;
                Console.WriteLine($"Functions API has {functionsCustomerCount} customers");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INFO: Functions API sync check failed: {ex.Message}");
                // Don't throw - this is secondary
            }
        }

        private async Task TryCreateInFunctionsApi(Customer customer)
        {
            try
            {
                Console.WriteLine("Syncing customer creation with Functions API...");
                var success = await _functionsApi.CreateCustomerAsync(customer);
                Console.WriteLine($"Functions API sync result: {success}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INFO: Functions API creation sync failed: {ex.Message}");
                // Don't throw - this is secondary
            }
        }

        private async Task TryUpdateInFunctionsApi(Customer customer)
        {
            try
            {
                Console.WriteLine("Syncing customer update with Functions API...");
                var success = await _functionsApi.UpdateCustomerAsync(customer);
                Console.WriteLine($"Functions API sync result: {success}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INFO: Functions API update sync failed: {ex.Message}");
                // Don't throw - this is secondary
            }
        }

        private async Task TryDeleteFromFunctionsApi(string rowKey)
        {
            try
            {
                Console.WriteLine("Syncing customer deletion with Functions API...");
                var success = await _functionsApi.DeleteCustomerAsync(rowKey);
                Console.WriteLine($"Functions API sync result: {success}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INFO: Functions API deletion sync failed: {ex.Message}");
                // Don't throw - this is secondary
            }
        }
    }
}