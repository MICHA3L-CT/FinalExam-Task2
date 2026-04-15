namespace E_commerce.Models
{
    public class Discount
    {
        public int DiscountId { get; set; }
        public string Code { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public int? MaxUses { get; set; }
        public int UsedCount { get; set; }
        public List<Order>? Orders { get; set; }
    }
}
