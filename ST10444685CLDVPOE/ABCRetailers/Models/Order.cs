using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ABCRetailers.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Orders";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name = "Order ID")]
        public string OrderId { get => RowKey; set { } }

        [Required]
        [Display(Name = "Customer ID")]
        public string CustomerId { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        // Store order items as JSON string
        public string OrderItemsJson { get; set; } = string.Empty;

        // Navigation property (not stored in table)
        [Display(Name = "Order Items")]
        public List<OrderItem> OrderItems
        {
            get
            {
                if (string.IsNullOrEmpty(OrderItemsJson))
                    return new List<OrderItem>();

                try
                {
                    return JsonSerializer.Deserialize<List<OrderItem>>(OrderItemsJson) ?? new List<OrderItem>();
                }
                catch
                {
                    return new List<OrderItem>();
                }
            }
            set
            {
                OrderItemsJson = JsonSerializer.Serialize(value ?? new List<OrderItem>());
            }
        }

        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Display(Name = "Customer Email")]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        // BACKWARD COMPATIBILITY: Keep old properties for existing code
        [Display(Name = "Product ID")]
        public string ProductId
        {
            get => OrderItems.FirstOrDefault()?.ProductId ?? string.Empty;
            set
            {
                if (!string.IsNullOrEmpty(value) && !OrderItems.Any())
                {
                    OrderItems.Add(new OrderItem { ProductId = value });
                }
            }
        }

        [Display(Name = "Product Name")]
        public string ProductName
        {
            get => OrderItems.FirstOrDefault()?.ProductName ?? string.Empty;
            set
            {
                var firstItem = OrderItems.FirstOrDefault();
                if (firstItem != null) firstItem.ProductName = value;
            }
        }

        [Display(Name = "Quantity")]
        public int Quantity
        {
            get => OrderItems.FirstOrDefault()?.Quantity ?? 0;
            set
            {
                var firstItem = OrderItems.FirstOrDefault();
                if (firstItem != null) firstItem.Quantity = value;
            }
        }

        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public decimal UnitPrice
        {
            get => OrderItems.FirstOrDefault()?.UnitPrice ?? 0;
            set
            {
                var firstItem = OrderItems.FirstOrDefault();
                if (firstItem != null) firstItem.UnitPrice = value;
            }
        }

        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice
        {
            get => OrderItems.FirstOrDefault()?.TotalPrice ?? TotalAmount;
            set => TotalAmount = value;
        }
    }

    public class OrderItem
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalPrice => UnitPrice * Quantity;

        public string? ImageUrl { get; set; }
    }
}