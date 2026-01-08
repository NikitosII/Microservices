using Coupon.API.Data;
using Coupon.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coupon.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : ControllerBase
    {
        private readonly CouponContext _context;
        private readonly ILogger<CouponsController> _logger;

        public CouponsController(CouponContext context, ILogger<CouponsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("validate")]
        public async Task<ActionResult<CouponResponse>> ValidateCoupon(CouponRequest request)
        {
            try
            {
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code == request.Code && c.IsActive);

                if (coupon == null)
                {
                    return Ok(new CouponResponse
                    {
                        IsValid = false,
                        Message = "Coupon not found"
                    });
                }

                var now = DateTime.UtcNow;
                if (now < coupon.ValidFrom || now > coupon.ValidUntil)
                {
                    return Ok(new CouponResponse
                    {
                        IsValid = false,
                        Message = "Coupon is expired or not yet valid"
                    });
                }

                if (coupon.UsedCount >= coupon.MaxUsageCount)
                {
                    return Ok(new CouponResponse
                    {
                        IsValid = false,
                        Message = "Coupon usage limit reached"
                    });
                }

                if (coupon.MinimumAmount.HasValue && request.OrderAmount < coupon.MinimumAmount.Value)
                {
                    return Ok(new CouponResponse
                    {
                        IsValid = false,
                        Message = $"Minimum order amount of {coupon.MinimumAmount.Value} required"
                    });
                }

                decimal discountAmount = coupon.DiscountAmount;

                if (coupon.DiscountType == "Percentage")
                {
                    discountAmount = request.OrderAmount * (coupon.DiscountAmount / 100);

                    if (coupon.MaximumDiscount.HasValue && discountAmount > coupon.MaximumDiscount.Value)
                    {
                        discountAmount = coupon.MaximumDiscount.Value;
                    }
                }

                return Ok(new CouponResponse
                {
                    IsValid = true,
                    DiscountAmount = discountAmount,
                    Message = "Coupon applied successfully",
                    Coupon = coupon
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500); 
            }
        }

        [HttpPost("{id}/use")]
        public async Task<IActionResult> UseCoupon(Guid id)
        {
            try
            {
                var coupon = await _context.Coupons.FindAsync(id);
                if (coupon == null)
                {
                    return NotFound();
                }

                if (coupon.UsedCount >= coupon.MaxUsageCount)
                {
                    return BadRequest("Coupon usage limit reached");
                }

                coupon.UsedCount++;
                coupon.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);

            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Coupons>>> GetCoupons()
        {
            try
            {
                var coupons = await _context.Coupons
                    .Where(c => c.IsActive)
                    .ToListAsync();
                return Ok(coupons);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<ActionResult<Coupons>> CreateCoupon(CouponCreateDto couponDto)
        {
            try
            {
                var coupon = new Coupons
                {
                    Id = Guid.NewGuid(),
                    Code = couponDto.Code,
                    Description = couponDto.Description,
                    DiscountAmount = couponDto.DiscountAmount,
                    MinimumAmount = couponDto.MinimumAmount,
                    ValidFrom = couponDto.ValidFrom,
                    ValidUntil = couponDto.ValidUntil,
                    MaxUsageCount = couponDto.MaxUsageCount,
                    UsedCount = 0,
                    IsActive = true,
                    DiscountType = couponDto.DiscountType,
                    MaximumDiscount = couponDto.MaximumDiscount,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Coupons.Add(coupon);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCoupon), new { id = coupon.Id }, coupon);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Coupons>> GetCoupon(Guid id)
        {
            try
            {
                var coupon = await _context.Coupons.FindAsync(id);

                if (coupon == null || !coupon.IsActive)
                {
                    return NotFound();
                }

                return Ok(coupon);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }
    }
}