namespace E_commerce.Models
{
    // Passed to the Error view to display diagnostic information when something goes wrong.
    // ASP.NET generates this automatically through the built-in error handling middleware.
    public class ErrorViewModel
    {
        // The request ID from the current HTTP activity, used to trace errors in logs
        public string? RequestId { get; set; }

        // Only show the request ID in the view if it actually has a value
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
