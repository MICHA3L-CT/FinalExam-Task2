namespace E_commerce.Models
{
    // Join table linking a Basket to a Product.
    // Each row represents one product line inside a basket, with its quantity and running total.
    public class BasketProduct
    {
        public int BasketProductId { get; set; }    // Primary key
        public int ProductId { get; set; }           // Foreign key pointing to the Product table
        public int BasketId { get; set; }            // Foreign key pointing to the Basket table
        public int Quantity { get; set; }            // Number of units of this product in the basket
        public decimal TotalPrice { get; set; }      // Quantity x unit price, kept in sync on every update

        // Navigation properties - allow EF Core to load the related Product and Basket objects
        public Product Product { get; set; }
        public Basket Basket { get; set; }
    }
}
