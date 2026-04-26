using FraudRiskApi.Models;
using FraudRiskApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FraudRiskApi.Controllers;

[ApiController]
[Route("api/v1/transactions")]
[Route("api/v1/transaction")]
public sealed class TransactionController : ControllerBase
{
    private readonly IFraudScoringService _fraudScoringService;
    private readonly IReportingService _reportingService;

    public TransactionController(
        IFraudScoringService fraudScoringService,
        IReportingService reportingService)
    {
        _fraudScoringService = fraudScoringService;
        _reportingService = reportingService;
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

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetTransactionById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var transaction = await _reportingService.GetTransactionByIdAsync(id, cancellationToken);
        return Ok(transaction);
    }

    [HttpGet("/api/v1/reports")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetReport(
        [FromQuery] DateTime? fromTimestamp,
        [FromQuery] DateTime? toTimestamp,
        [FromQuery] string? riskLevel,
        CancellationToken cancellationToken)
    {
        var transactions = await _reportingService.GetTransactionsReportAsync(
            fromTimestamp,
            toTimestamp,
            riskLevel,
            cancellationToken);

        return Ok(transactions);
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
