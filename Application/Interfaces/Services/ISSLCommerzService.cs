namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface ISSLCommerzService
    {
        /// <summary>
        /// Initiates a payment session with SSLCommerz.
        /// Returns the GatewayPageURL to redirect the user to, or null on failure.
        /// </summary>
        Task<string?> InitiatePaymentAsync(
            string  tranId,
            decimal amount,
            string  successUrl,
            string  failUrl,
            string  cancelUrl,
            string  ipnUrl,
            string  customerName,
            string  customerEmail,
            string  customerPhone,
            string  productName);

        /// <summary>
        /// Validates a payment using SSLCommerz's validation API.
        /// Returns (isValid, tranId) — tranId is the original transaction ID we sent.
        /// </summary>
        Task<(bool IsValid, string TranId)> ValidatePaymentAsync(string valId);
    }
}
