namespace Bachelor_s_Point.Infrastructure.Settings
{
    public class SSLCommerzSettings
    {
        public string StoreId       { get; set; } = string.Empty;
        public string StorePassword { get; set; } = string.Empty;
        public bool   IsSandbox     { get; set; } = true;
        /// <summary>
        /// Public base URL of your app — needed so SSLCommerz can call back
        /// your success/fail/cancel/IPN endpoints.
        /// e.g. "https://yourdomain.com" or "https://localhost:7141" for dev.
        /// </summary>
        public string BaseUrl       { get; set; } = "https://localhost:7141";
    }
}
