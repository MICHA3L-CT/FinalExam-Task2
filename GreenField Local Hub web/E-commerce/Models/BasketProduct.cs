namespace E_commerce.Models
{
    public class BasketProduct
    {
        public int BasketProductId { get; set; }
        public int ProductId { get; set; }
        public int BasketId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public Product Product { get; set; }
        public Basket Basket { get; set; }
    }
}
