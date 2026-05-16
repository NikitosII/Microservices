using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orchestrator.API.Data;

namespace Orchestrator.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/saga")]
public class SagaController : ControllerBase
{
    private readonly OrchestratorContext _context;

    public SagaController(OrchestratorContext context) => _context = context;

    [HttpGet("orders/{correlationId:guid}")]
    public async Task<IActionResult> GetOrderSagaStatus(Guid correlationId)
    {
        var saga = await _context.OrderSagas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CorrelationId == correlationId);

        if (saga is null)
            return NotFound(new { message = "Saga not found or already finalized" });

        return Ok(new
        {
            saga.CorrelationId,
            saga.CurrentState,
            saga.OrderId,
            saga.FailureReason,
            saga.CreatedAt,
            saga.UpdatedAt
        });
    }
}
