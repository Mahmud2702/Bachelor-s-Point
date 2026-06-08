namespace Bachelor_s_Point.Infrastructure.Settings
{
    public class PaymentSettings
    {
        public string ReceiverName   { get; set; } = "Bachelor's Point";
        public string ReceiverNumber { get; set; } = string.Empty;
        public string ReceiverMethod { get; set; } = "bKash";
        public decimal RegistrationFee { get; set; } = 20;
        public int RoomFeePercent    { get; set; } = 20;
    }
}
