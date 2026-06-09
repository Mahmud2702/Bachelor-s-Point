using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Bachelor_s_Point.Services
{
    public class SSLCommerzService : ISSLCommerzService
    {
        private readonly HttpClient _http;
        private readonly SSLCommerzSettings _settings;
        private readonly ILogger<SSLCommerzService> _logger;

        private string InitUrl => _settings.IsSandbox
            ? "https://sandbox.sslcommerz.com/gwprocess/v4/api.php"
            : "https://securepay.sslcommerz.com/gwprocess/v4/api.php";

        private string ValidateUrl => _settings.IsSandbox
            ? "https://sandbox.sslcommerz.com/validator/api/validationserverAPI.php"
            : "https://securepay.sslcommerz.com/validator/api/validationserverAPI.php";

        public SSLCommerzService(
            HttpClient http,
            IOptions<SSLCommerzSettings> settings,
            ILogger<SSLCommerzService> logger)
        {
            _http     = http;
            _settings = settings.Value;
            _logger   = logger;
        }

        public async Task<string?> InitiatePaymentAsync(
            string  tranId,
            decimal amount,
            string  successUrl,
            string  failUrl,
            string  cancelUrl,
            string  ipnUrl,
            string  customerName,
            string  customerEmail,
            string  customerPhone,
            string  productName)
        {
            var form = new Dictionary<string, string>
            {
                ["store_id"]         = _settings.StoreId,
                ["store_passwd"]     = _settings.StorePassword,
                ["total_amount"]     = amount.ToString("0.00"),
                ["currency"]         = "BDT",
                ["tran_id"]          = tranId,
                ["success_url"]      = successUrl,
                ["fail_url"]         = failUrl,
                ["cancel_url"]       = cancelUrl,
                ["ipn_url"]          = ipnUrl,
                ["cus_name"]         = string.IsNullOrWhiteSpace(customerName) ? "Customer" : customerName,
                ["cus_email"]        = string.IsNullOrWhiteSpace(customerEmail) ? "customer@email.com" : customerEmail,
                ["cus_phone"]        = string.IsNullOrWhiteSpace(customerPhone) ? "01700000000" : customerPhone,
                ["cus_add1"]         = "Bangladesh",
                ["cus_city"]         = "Dhaka",
                ["cus_country"]      = "Bangladesh",
                ["shipping_method"]  = "NO",
                ["product_name"]     = productName,
                ["product_category"] = "service",
                ["product_profile"]  = "service",
            };

            try
            {
                var response = await _http.PostAsync(InitUrl, new FormUrlEncodedContent(form));
                var json     = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("SSLCommerz initiate response: {json}", json);

                var data = JsonSerializer.Deserialize<JsonElement>(json);

                if (data.TryGetProperty("status", out var statusProp) &&
                    statusProp.GetString() == "SUCCESS" &&
                    data.TryGetProperty("GatewayPageURL", out var urlProp))
                {
                    return urlProp.GetString();
                }

                _logger.LogWarning("SSLCommerz initiate failed. Response: {json}", json);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSLCommerz initiate exception for TranId={TranId}", tranId);
                return null;
            }
        }

        public async Task<(bool IsValid, string TranId)> ValidatePaymentAsync(string valId)
        {
            string url = $"{ValidateUrl}?val_id={Uri.EscapeDataString(valId)}" +
                         $"&store_id={Uri.EscapeDataString(_settings.StoreId)}" +
                         $"&store_passwd={Uri.EscapeDataString(_settings.StorePassword)}" +
                         $"&format=json";
            try
            {
                var json = await _http.GetStringAsync(url);
                _logger.LogInformation("SSLCommerz validate response: {json}", json);

                var data   = JsonSerializer.Deserialize<JsonElement>(json);
                var status = data.TryGetProperty("status", out var s) ? s.GetString() : "";
                var tranId = data.TryGetProperty("tran_id", out var t) ? t.GetString() ?? "" : "";

                bool isValid = status == "VALID" || status == "VALIDATED";
                return (isValid, tranId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSLCommerz validate exception for ValId={ValId}", valId);
                return (false, "");
            }
        }
    }
}
