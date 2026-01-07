namespace Coupon.API.Models
{
    public class CouponRequest
    {
        public string Code { get; set; } = string.Empty;
        public decimal OrderAmount { get; set; }
    }

}

