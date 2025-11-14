using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Models
{
    public class Product : ITableEntity
    {
        // PartitionKey for Azure Table
        public string PartitionKey { get; set; } = "PRODUCTS";

        // RowKey for Azure Table
        public string RowKey { get; set; } = string.Empty;

        // Optional convenience property
        public string ProductId
        {
            get => RowKey;
            set => RowKey = value;
        }

        // Product properties
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockAvailable { get; set; }
        public string ProductImageUrl { get; set; } = string.Empty;

        // FIX: Changed to nullable DateTimeOffset?
        public DateTimeOffset? Timestamp { get; set; }

        // MUST match ITableEntity exactly: ETag type
        public ETag ETag { get; set; }
    }
}