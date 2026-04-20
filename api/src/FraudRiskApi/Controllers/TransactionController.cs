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
}
