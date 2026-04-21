namespace E_commerce.Models
{
    // Represents a promotional discount code that can be linked to an order.
    // Currently the loyalty system applies discounts automatically rather than via codes,
    // but this model supports manual discount codes for future use.
    public class Discount
    {
        public int DiscountId { get; set; }             // Primary key
        public string Code { get; set; }                 // The discount code string (e.g. "SAVE10")
        public decimal DiscountPercentage { get; set; }  // Percentage off the order total (e.g. 10 = 10%)
        public DateTime ExpiryDate { get; set; }         // Date after which the code is no longer valid
        public bool IsActive { get; set; }               // Whether the code is currently enabled
        public int? MaxUses { get; set; }                // Optional cap on how many times the code can be used
        public int UsedCount { get; set; }               // How many times the code has been used so far

        // Navigation property - all orders that used this discount
        public List<Order>? Orders { get; set; }
    }
}
