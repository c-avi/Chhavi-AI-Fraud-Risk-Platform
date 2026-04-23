using FraudRiskApi.Models;
using FraudRiskApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FraudRiskApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class TransactionController : ControllerBase
{
    private readonly IFraudScoringService _fraudScoringService;

    public TransactionController(IFraudScoringService fraudScoringService)
    {
        _fraudScoringService = fraudScoringService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RiskScoreResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskScoreResponse>> PostTransaction(
        [FromBody] TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _fraudScoringService.ScoreTransactionAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(RiskSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _fraudScoringService.GetRiskSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionAlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionAlertResponse>>> GetAlerts(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var alerts = await _fraudScoringService.GetRecentAlertsAsync(limit, cancellationToken);
        return Ok(alerts);
    }
}
