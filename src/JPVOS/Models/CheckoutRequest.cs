namespace JPVOS.Models
{
    public class CheckoutRequest
    {
        public required string PackageKey { get; set; }
        public string? SuccessUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}
