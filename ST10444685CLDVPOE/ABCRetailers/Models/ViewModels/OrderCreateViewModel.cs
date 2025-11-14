using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ABCRetailers.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required]
        [Display(Name = "Customer ID")]
        public string CustomerId { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Product ID")]
        public string ProductId { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice => UnitPrice * Quantity;

        // Add this missing property
        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount => OrderItems?.Sum(item => item.TotalPrice) ?? TotalPrice;

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Display(Name = "Customer Email")]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        // For multiple order items
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Dropdown lists
        public List<Customer> Customers { get; set; } = new List<Customer>();
        public List<Product> Products { get; set; } = new List<Product>();
    }
}