// Models/FileUploadModel.cs
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class FileUploadModel
    {
        [Required(ErrorMessage = "Please select a file to upload.")]
        [Display(Name = "Proof of Payment")]
        public IFormFile ProofOfPayment { get; set; } = null!;

        [Display(Name = "Order ID")]
        public string? OrderId { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }
    }
}