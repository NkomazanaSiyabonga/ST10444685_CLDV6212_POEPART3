namespace ABCRetailers.Models.ViewModels
{
    public class CartItemViewModel
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; } // Added UnitPrice
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Price * Quantity;
        public int StockAvailable { get; set; }
    }

    public class CartViewModel
    {
        public string CartId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal GrandTotal => Items.Sum(item => item.TotalPrice);
        public int TotalItems => Items.Sum(item => item.Quantity);
    }

    // Add this if you're using CartPageViewModel
    public class CartPageViewModel
    {
        public CartViewModel Cart { get; set; } = new CartViewModel();
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
    }
}