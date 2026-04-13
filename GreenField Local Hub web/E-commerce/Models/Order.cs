namespace E_commerce.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string UserId { get; set; }
        public int? DiscountId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } 
        public decimal TotalAmount { get; set; }
        public string DeliveryAddress { get; set; } 
        public decimal ShippingFee { get; set; }
        public string? DeliveryType { get; set; }
        public DateTime? ScheduleDate { get; set; }
        public Discount? Discount { get; set; }
        public List<OrderProduct>? OrderProducts { get; set; }
        
    }
}
