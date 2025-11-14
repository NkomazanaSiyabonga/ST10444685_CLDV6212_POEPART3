using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;

namespace ABCRetailers.Services
{
    public interface IFunctionsApi
    {
        // Customer methods
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<Customer?> GetCustomerByUsernameAsync(string username);
        Task<bool> CreateCustomerAsync(Customer customer);
        Task<bool> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(string id);

        // Product methods
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string id);
        Task<bool> CreateProductAsync(Product product, IFormFile? imageFile);
        Task<bool> UpdateProductAsync(string id, Product product, IFormFile? imageFile);
        Task<bool> DeleteProductAsync(string id);

        // Order methods
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string id);
        Task<bool> CreateOrderAsync(OrderCreateViewModel orderModel);
        Task<bool> UpdateOrderAsync(Order order);
        Task<bool> DeleteOrderAsync(string id);
        Task<bool> UpdateOrderStatusAsync(string id, string newStatus);
        Task<bool> UpdateOrderStatusOnlyAsync(string id, string newStatus); // ADD THIS LINE
        Task<List<Order>> GetOrdersByCustomerIdAsync(string customerId);

        // File upload methods
        Task<string?> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName);
        Task<string?> UploadFileAsync(IFormFile file, string containerName);
    }
}