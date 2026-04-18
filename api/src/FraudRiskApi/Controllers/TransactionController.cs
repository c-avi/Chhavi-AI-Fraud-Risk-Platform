using FraudRiskApi.Models;
using FraudRiskApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FraudRiskApi.Controllers;

[ApiController]
[Route("api/v1/transactions")]
public sealed class TransactionController : ControllerBase
{
    private readonly IFraudScoringEngine _fraudScoringEngine;

    public TransactionController(IFraudScoringEngine fraudScoringEngine)
    {
        _fraudScoringEngine = fraudScoringEngine;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RiskScoreResponse), StatusCodes.Status200OK)]
    public ActionResult<RiskScoreResponse> PostTransaction([FromBody] TransactionRequest request)
    {
        var result = _fraudScoringEngine.CalculateScore(request);
        return Ok(result);
    }
}
