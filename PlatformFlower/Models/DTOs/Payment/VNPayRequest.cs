namespace PlatformFlower.Models.DTOs.Payment
{
    public class VNPayRequest
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = null!;
        public string ReturnUrl { get; set; } = null!;
    }

    public class VNPayResponse
    {
        public bool Success { get; set; }
        public string PaymentUrl { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    public class VNPayReturnRequest
    {
        public string vnp_Amount { get; set; } = null!;
        public string vnp_BankCode { get; set; } = null!;
        public string vnp_BankTranNo { get; set; } = null!;
        public string vnp_CardType { get; set; } = null!;
        public string vnp_OrderInfo { get; set; } = null!;
        public string vnp_PayDate { get; set; } = null!;
        public string vnp_ResponseCode { get; set; } = null!;
        public string vnp_TmnCode { get; set; } = null!;
        public string vnp_TransactionNo { get; set; } = null!;
        public string vnp_TransactionStatus { get; set; } = null!;
        public string vnp_TxnRef { get; set; } = null!;
        public string vnp_SecureHash { get; set; } = null!;
    }
}
