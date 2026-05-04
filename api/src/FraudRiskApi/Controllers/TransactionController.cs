using FraudRiskApi.Models;
using FraudRiskApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FraudRiskApi.Controllers;

[ApiController]
[Route("api/v1/transactions")]
[Route("api/v1/transaction")]
public sealed class TransactionController : ControllerBase
{
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

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
    [ProducesResponseType(typeof(AcceptedTransactionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostTransaction(
        [FromBody] TransactionRequest request,
        CancellationToken cancellationToken,
        [FromHeader(Name = IdempotencyKeyHeaderName)] string? idempotencyKey = null)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problem(
                title: "Missing Idempotency-Key",
                detail: $"The {IdempotencyKeyHeaderName} header is required for transaction submissions.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        BeginScoringResult result;
        try
        {
            result = await _fraudScoringService.SubmitTransactionForScoringAsync(
                request,
                idempotencyKey.Trim(),
                cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
        }

        return result.Kind switch
        {
            BeginScoringKind.Conflict => Conflict(
                new ProblemDetails
                {
                    Title = "Idempotency conflict",
                    Detail = "The Idempotency-Key was already used with a different request payload.",
                    Status = StatusCodes.Status409Conflict
                }),
            BeginScoringKind.CompletedReplay when result.CompletedScore is not null => Ok(result.CompletedScore),
            BeginScoringKind.CreatedPending or BeginScoringKind.AlreadyPending => BuildAccepted(result.TransactionId),
            _ => throw new InvalidOperationException("Unexpected fraud scoring submission outcome.")
        };
    }

    private IActionResult BuildAccepted(int transactionId)
    {
        var pathBase = Request.PathBase.HasValue ? Request.PathBase.Value! : string.Empty;
        var statusUrl = $"{Request.Scheme}://{Request.Host}{pathBase}/api/v1/transactions/{transactionId}";
        Response.Headers.Append("Location", statusUrl);

        return StatusCode(
            StatusCodes.Status202Accepted,
            new AcceptedTransactionResponse
            {
                TransactionId = transactionId,
                Status = "accepted",
                StatusUrl = statusUrl
            });
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
    [Obsolete("Use GET /api/v1/alerts for persisted high-risk alerts.")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionAlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionAlertResponse>>> GetAlerts(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var alerts = await _fraudScoringService.GetRecentAlertsAsync(limit, cancellationToken);
        return Ok(alerts);
    }
}
