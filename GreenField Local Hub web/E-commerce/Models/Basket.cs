namespace E_commerce.Models
{
    // Represents a shopping basket belonging to a user.
    // A basket stays open (Status = true) until the user places an order,
    // at which point it is closed (Status = false) and a new one is created next visit.
    public class Basket
    {
        public int BasketId { get; set; }           // Primary key, auto-incremented by the database
        public string UserId { get; set; }           // The Identity user this basket belongs to
        public bool Status { get; set; }             // True = basket is open, False = basket is closed after checkout
        public DateTime CreatedDate { get; set; }    // When this basket record was first created

        // Navigation property - gives access to the full list of products in this basket
        public List<BasketProduct>? BasketProducts { get; set; }
    }
}
