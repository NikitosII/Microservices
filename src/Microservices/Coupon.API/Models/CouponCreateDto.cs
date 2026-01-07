
namespace Coupon.API.Models
{
    public class CouponCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public decimal? MinimumAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public int MaxUsageCount { get; set; }
        public string DiscountType { get; set; } = "Fixed";
        public decimal? MaximumDiscount { get; set; }

    }
}


