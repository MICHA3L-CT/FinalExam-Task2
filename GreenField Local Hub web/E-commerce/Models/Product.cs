namespace E_commerce.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public int  ProducerId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public string? ImagePath { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; }
        public DateOnly DateAdded { get; set; }
        public Producer Producer { get; set; }
        public List<BasketProduct>? BasketProducts { get; set; }
        public List<OrderProduct>? OrderProducts { get; set; }
    }
}
