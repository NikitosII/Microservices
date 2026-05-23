using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orchestrator.API.Data;

namespace Orchestrator.API.Controllers;

/// <summary>
/// Exposes saga status for client-side polling after an order is submitted.
/// Route: /api/v1/saga
/// <list type="bullet">
///   <item>GET orders/{correlationId} — returns CurrentState, OrderId, FailureReason, and timestamps.
///         Returns 404 when the saga has already finalized and been removed from the repository.</item>
/// </list>
/// </summary>
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
