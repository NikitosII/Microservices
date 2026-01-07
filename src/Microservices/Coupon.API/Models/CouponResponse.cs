namespace Coupon.API.Models
{
    public class CouponResponse
    {
        public bool IsValid { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Message { get; set; } = string.Empty;
        public Coupons? Coupon { get; set; }
    }
}
