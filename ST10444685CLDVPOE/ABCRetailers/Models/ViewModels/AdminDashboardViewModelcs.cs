using System.Collections.Generic;
using ABCRetailers.Models;

namespace ABCRetailers.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int CustomerCount { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
    }
}