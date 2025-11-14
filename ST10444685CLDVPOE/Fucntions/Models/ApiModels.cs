using System.Text.Json.Serialization;

namespace ABCRetailers.Functions.Models
{
    public class CustomerDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("surname")] public string Surname { get; set; } = string.Empty;
        [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
        [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
        [JsonPropertyName("shippingAddress")] public string ShippingAddress { get; set; } = string.Empty;
        [JsonPropertyName("partitionKey")] public string PartitionKey { get; set; } = "CUSTOMER";
        [JsonPropertyName("rowKey")] public string? RowKey { get; set; }
    }

    public class ProductDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("productName")] public string ProductName { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
        [JsonPropertyName("price")] public double Price { get; set; }
        [JsonPropertyName("stockAvailable")] public int StockAvailable { get; set; }
        [JsonPropertyName("productImageUrl")] public string ProductImageUrl { get; set; } = string.Empty;
        [JsonPropertyName("partitionKey")] public string PartitionKey { get; set; } = "Products";
        [JsonPropertyName("rowKey")] public string? RowKey { get; set; }
    }

    public class OrderDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("customerId")] public string CustomerId { get; set; } = string.Empty;
        [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
        [JsonPropertyName("productId")] public string ProductId { get; set; } = string.Empty;
        [JsonPropertyName("productName")] public string ProductName { get; set; } = string.Empty;
        [JsonPropertyName("orderDate")] public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
        [JsonPropertyName("unitPrice")] public double UnitPrice { get; set; }
        [JsonPropertyName("totalPrice")] public double TotalPrice { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = "Submitted";
        [JsonPropertyName("partitionKey")] public string PartitionKey { get; set; } = "Orders";
        [JsonPropertyName("rowKey")] public string? RowKey { get; set; }
    }

    public class OrderQueueMessage
    {
        [JsonPropertyName("orderId")] public string OrderId { get; set; } = string.Empty;
        [JsonPropertyName("customerId")] public string CustomerId { get; set; } = string.Empty;
        [JsonPropertyName("customerName")] public string CustomerName { get; set; } = string.Empty;
        [JsonPropertyName("productName")] public string ProductName { get; set; } = string.Empty;
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
        [JsonPropertyName("totalPrice")] public double TotalPrice { get; set; }
        [JsonPropertyName("orderDate")] public DateTime OrderDate { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = "Submitted";
    }

    public class FileUploadDto
    {
        [JsonPropertyName("fileName")] public string FileName { get; set; } = string.Empty;
        [JsonPropertyName("containerName")] public string ContainerName { get; set; } = string.Empty;
        [JsonPropertyName("contentType")] public string ContentType { get; set; } = string.Empty;
        [JsonPropertyName("fileData")] public string FileData { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public T? Data { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }
}