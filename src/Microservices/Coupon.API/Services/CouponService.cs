using Coupon.API.Data;
using Coupon.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Coupon.API.Services
{
    public interface ICouponService
    {
        Task<IEnumerable<Coupons>> GetCouponsAsync();
        Task<Coupons?> GetByIdAsync(Guid id);
        Task<Coupons?> GetByCodeAsync(string code);
        Task<Coupons> CreateCouponAsync(CouponCreateDto couponDto);
        Task<Coupons?> UpdateCouponAsync(Guid id, Coupons coupon);
        Task<bool> DeleteCouponAsync(Guid id);
        Task<CouponResponse> ValidateCouponAsync(string code, decimal orderAmount);
        Task<bool> UseCouponAsync(Guid couponId);
        Task<IEnumerable<Coupons>> GetActiveCouponsAsync();
    }
    public class CouponService : ICouponService
    {
        private readonly CouponContext _context;
        private readonly ILogger<CouponService> _logger;

        public CouponService(CouponContext context, ILogger<CouponService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Coupons>> GetCouponsAsync()
        {
            try
            {
                return await _context.Coupons
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                throw;
            }
        }

        public async Task<Coupons?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.Coupons.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                throw;
            }
        }

        public async Task<Coupons?> GetByCodeAsync(string code)
        {
            try
            {
                return await _context.Coupons.FirstOrDefaultAsync(x => x.Code == code);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                throw;
            }
        }

        public async Task<Coupons> CreateCouponAsync(CouponCreateDto dto)
        {
            try
            {
                var coupon = new Coupons
                {
                    Id = Guid.NewGuid(),
                    Code = dto.Code,
                    Description = dto.Description,
                    MinimumAmount = dto.MinimumAmount,
                    ValidFrom = dto.ValidFrom,
                    ValidUntil = dto.ValidUntil,
                    MaxUsageCount = dto.MaxUsageCount,
                    UsedCount = 0,
                    IsActive = true,
                    DiscountType = dto.DiscountType,
                    MaximumDiscount = dto.MaximumDiscount,
                    CreatedAt = DateTime.Now
                };
                _context.Coupons.Add(coupon);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Coupon created: {CouponId} - {Code}", coupon.Id, coupon.Code);
                return coupon;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                throw;
            }
        }

        public async Task<Coupons?> UpdateCouponAsync(Guid id, Coupons coupon)
        {
            try
            {
                var exist = await _context.Coupons.FindAsync(id);
                if(exist == null)
                {
                    return null;
                }
                exist.Code = coupon.Code;
                exist.Description = coupon.Description;
                exist.DiscountAmount = coupon.DiscountAmount;
                exist.MinimumAmount = coupon.MinimumAmount;
                exist.ValidFrom = coupon.ValidFrom;
                exist.ValidUntil = coupon.ValidUntil;
                exist.MaxUsageCount = coupon.MaxUsageCount;
                exist.IsActive = coupon.IsActive;
                exist.DiscountType = coupon.DiscountType;
                exist.MaximumDiscount = coupon.MaximumDiscount;
                exist.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Coupon updated: {CouponId", id);
                return exist;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteCouponAsync(Guid id)
        {
            try
            {
                var coupon = await _context.Coupons.FindAsync(id);
                if (coupon == null)
                {
                    return false;
                }

                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Coupon deleted: {CouponId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                throw;
            }
        }

        public async Task<CouponResponse> ValidateCouponAsync(string code, decimal orderAmount)
        {
            try
            {
                var coupon = await GetByCodeAsync(code);

                if (coupon == null)
                {
                    return new CouponResponse
                    {
                        IsValid = false,
                        Message = "Coupon not found"
                    };
                }

                if (!coupon.IsActive)
                {
                    return new CouponResponse
                    {
                        IsValid = false,
                        Message = "Coupon is not active"
                    };
                }

                var now = DateTime.UtcNow;
                if (now < coupon.ValidFrom)
                {
                    return new CouponResponse
                    {
                        IsValid = false,
                        Message = $"Coupon is not valid until {coupon.ValidFrom:yyyy-MM-dd}"
                    };
                }

                if (now > coupon.ValidUntil)
                {
                    return new CouponResponse
                    {
                        IsValid = false,
                        Message = "Coupon has expired"
                    };
                }

                if (coupon.UsedCount >= coupon.MaxUsageCount)
                {
                    return new CouponResponse
                    {
                        IsValid = false,
                        Message = "Coupon usage limit reached"
                    };
                }

                if (coupon.MinimumAmount.HasValue && orderAmount < coupon.MinimumAmount.Value)
                {
                    return new CouponResponse
                    {
                        IsValid = false,
                        Message = $"Minimum order amount of {coupon.MinimumAmount.Value:C} required"
                    };
                }

                decimal discountAmount = CalculateDiscount(coupon, orderAmount);

                return new CouponResponse
                {
                    IsValid = true,
                    DiscountAmount = discountAmount,
                    Message = "Coupon applied successfully",
                    Coupon = coupon
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating coupon {Code}", code);
                return new CouponResponse
                {
                    IsValid = false,
                    Message = "Error validating coupon"
                };
            }
        }

        public async Task<bool> UseCouponAsync(Guid couponId)
        {
            try
            {
                var coupon = await _context.Coupons.FindAsync(couponId);
                if (coupon == null)
                {
                    return false;
                }

                if (coupon.UsedCount >= coupon.MaxUsageCount)
                {
                    return false;
                }

                coupon.UsedCount++;
                coupon.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Coupon used: {CouponId}, now used {UsedCount}/{MaxUsageCount}",
                    couponId, coupon.UsedCount, coupon.MaxUsageCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Coupons>> GetActiveCouponsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;

                return await _context.Coupons
                    .Where(c => c.IsActive &&
                           c.ValidFrom <= now &&
                           c.ValidUntil >= now &&
                           c.UsedCount < c.MaxUsageCount)
                    .OrderByDescending(c => c.DiscountAmount)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                throw;
            }
        }

        private decimal CalculateDiscount(Coupons coupon, decimal orderAmount)
        {
            decimal discount = 0;

            if (coupon.DiscountType == "Percentage")
            {
                discount = orderAmount * (coupon.DiscountAmount / 100);

                if (coupon.MaximumDiscount.HasValue && discount > coupon.MaximumDiscount.Value)
                {
                    discount = coupon.MaximumDiscount.Value;
                }
            }
            else if (coupon.DiscountType == "Fixed")
            {
                discount = coupon.DiscountAmount;
            }

            return discount;
        }
    }
}


