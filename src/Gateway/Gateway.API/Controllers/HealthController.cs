using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase 
    {
        private readonly ILogger<HealthController> _logger;
        public HealthController(ILogger<HealthController> logger){
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("HealthCheck");
            var info = new
            {
                Status = "Healthy",
                Time = DateTime.Now,
                Service = "Gateway.API",
                Version = "1.0.0"
            };
            return Ok(info);

        }

        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            try
            {
                _logger.LogInformation($"{nameof(Ready)}");
                return Ok(new 
                { 
                    Status = "Ready",
                    Time = DateTime.Now,
                });

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.Message);
                return StatusCode(503, new { Status = "Not Ready", Error = ex.Message });
            }
        }

        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new
            {
                Status = "Alive",
                Time = DateTime.Now,
            });
        }
    }


}
