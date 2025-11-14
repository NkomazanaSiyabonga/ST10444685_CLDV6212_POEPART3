namespace ABCRetailers.Models
{
    public class UpdateOrderDto
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public string Status { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
