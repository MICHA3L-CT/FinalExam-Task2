namespace E_commerce.Models
{
    public class Basket
    {
        public int BasketId { get; set; }
        public string UserId { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<BasketProduct>? BasketProducts { get; set; }
    }
}
