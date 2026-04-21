namespace E_commerce.Models
{
    // Represents a product listed for sale on the platform by a producer.
    public class Product
    {
        public int ProductId { get; set; }          // Primary key
        public int ProducerId { get; set; }          // Foreign key - which producer is selling this
        public string ProductName { get; set; }      // Name shown on the product card
        public string Description { get; set; }      // Full description of the product
        public string? ImagePath { get; set; }       // Relative URL to the product image, e.g. "/Images/apples.jpg"
        public decimal Price { get; set; }           // Unit price in GBP
        public int StockQuantity { get; set; }       // Current stock level, decremented when an order is placed
        public string Category { get; set; }         // Product category, e.g. "Fruit", "Vegetables", "Dairy"
        public DateOnly DateAdded { get; set; }      // When the product was first listed
        public bool IsActive { get; set; } = true;   // Whether the product is visible and purchasable

        // Navigation properties - give access to the producer and any basket/order lines this product appears in
        public Producer Producer { get; set; }
        public List<BasketProduct>? BasketProducts { get; set; }
        public List<OrderProduct>? OrderProducts { get; set; }
    }
}
