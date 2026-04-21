using System;
using System.Collections.Generic;

namespace E_commerce.Models
{
    // Represents a local farmer or supplier registered on the platform.
    // Each Producer is linked to an Identity user account via UserId.
    public class Producer
    {
        public int ProducerId { get; set; }                     // Primary key
        public string UserId { get; set; } = null!;             // Links to the ASP.NET Identity user who owns this profile
        public string ProducerName { get; set; } = null!;       // Display name of the farm or producer
        public string PhoneNumber { get; set; } = null!;        // Contact phone number
        public string? ProductDescription { get; set; }         // Short description of what they produce
        public string Location { get; set; } = null!;           // Town or city where the producer is based
        public string? ProducerInfo { get; set; }               // Longer about-us text shown on the producer card
        public DateOnly DateJoined { get; set; }                // When the producer registered on the platform
        public bool IsVerified { get; set; }                    // True if the producer has been verified by admin

        // Navigation property - all products listed by this producer
        public List<Product>? Products { get; set; }
    }
}
