namespace E_commerce.Models
{
    // Join table between Order and Product.
    // Each row is one product line on an order, storing a snapshot of the price at time of purchase.
    public class OrderProduct
    {
        public int OrderProductId { get; set; }     // Primary key
        public int OrderId { get; set; }             // Foreign key to the parent Order
        public int ProductId { get; set; }           // Foreign key to the Product that was ordered
        public decimal UnitPrice { get; set; }       // Price per unit at the time of purchase (snapshot, not live price)
        public int Quantity { get; set; }            // Number of units ordered

        // Navigation properties - give access to the full Order and Product objects
        public Order Order { get; set; }
        public Product Product { get; set; }
    }
}
