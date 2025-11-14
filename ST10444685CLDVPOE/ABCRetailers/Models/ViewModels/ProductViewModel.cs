using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class ProductViewModel
    {
        public string? Id { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public double Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock must be at least 0")]
        public int StockAvailable { get; set; }

        public IFormFile? Image { get; set; } // ✅ only here
        public string? ExistingImageUrl { get; set; }
    }
}
