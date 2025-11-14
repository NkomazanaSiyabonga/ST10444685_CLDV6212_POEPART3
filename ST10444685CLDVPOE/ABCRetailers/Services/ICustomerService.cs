using ABCRetailers.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetailers.Services
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerAsync(string rowKey);
        Task<bool> CreateCustomerAsync(Customer customer);
        Task<bool> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(string rowKey);
    }
}