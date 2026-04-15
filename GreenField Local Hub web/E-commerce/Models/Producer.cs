using System;
using System.Collections.Generic;

namespace E_commerce.Models
{
    public class Producer
    {
        public int ProducerId { get; set; }
        public string UserId { get; set; } = null!;
        public string ProducerName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? ProductDescription { get; set; }
        public string Location { get; set; } = null!;
        public string? ProducerInfo { get; set; }
        public DateOnly DateJoined { get; set; }
        public bool IsVerified { get; set; }
        public List<Product>? Products { get; set; }
    }
}
