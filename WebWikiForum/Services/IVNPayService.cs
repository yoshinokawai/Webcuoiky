using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace WebWikiForum.Services
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(HttpContext context, PaymentInformationModel model);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }

    public class PaymentInformationModel
    {
        public string OrderType { get; set; } = "other";
        public double Amount { get; set; }
        public string OrderDescription { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int DonationId { get; set; }
    }

    public class PaymentResponseModel
    {
        public string OrderDescription { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string VnPayResponseCode { get; set; } = string.Empty;
        public int DonationId { get; set; }
    }
}
