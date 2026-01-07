namespace Coupon.API.Models
{
    public class Coupons
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public decimal? MinimumAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public int MaxUsageCount { get; set; }
        public int UsedCount { get; set; }
        public bool IsActive { get; set; } = true;
        public string DiscountType { get; set; } = "Fixed"; // Fixed or Percentage
        public decimal? MaximumDiscount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}


