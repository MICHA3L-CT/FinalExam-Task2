using System.ComponentModel.DataAnnotations.Schema;

namespace E_commerce.Models
{
    // Represents a completed or in-progress customer order.
    // Created when a user checks out from their basket.
    public class Order
    {
        public int OrderId { get; set; }                // Primary key
        public string UserId { get; set; }               // The customer who placed this order
        public int? DiscountId { get; set; }             // Optional link to a Discount code applied at checkout
        public DateTime OrderDate { get; set; }          // When the order was placed
        public string OrderStatus { get; set; }          // Current status: Pending, Processing, Shipped, Delivered, Cancelled
        public decimal TotalAmount { get; set; }         // Final price including shipping and any discount
        public string? DeliveryAddress { get; set; }     // Street address for delivery orders, null for collection
        public decimal ShippingFee { get; set; }         // Delivery cost added at checkout (0 for collection)
        public string? DeliveryType { get; set; }        // "Next Day", "First Class", "Standard", or "Collection"
        public DateTime? ScheduleDate { get; set; }      // Chosen collection date for click-and-collect orders

        // Navigation properties - let us load the related Discount and list of order lines
        public Discount? Discount { get; set; }
        public List<OrderProduct>? OrderProducts { get; set; }

        // Not saved to the database - only used in the checkout form to carry the delivery/collection choice
        [NotMapped]
        public string? OrderMethod { get; set; }         // "delivery" or "collection" from the checkout form
    }
}
