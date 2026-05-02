using FraudRiskApi.Models;
using FraudRiskApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FraudRiskApi.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AiController : ControllerBase
{
    private readonly IAiPredictionService _aiPredictionService;
    private readonly ILogger<AiController> _logger;

    public AiController(IAiPredictionService aiPredictionService, ILogger<AiController> logger)
    {
        _aiPredictionService = aiPredictionService;
        _logger = logger;
    }

    [HttpPost("predict")]
    [ProducesResponseType(typeof(AiPredictionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AiPredictionResponse>> Predict(
        [FromBody] AiPredictionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received AI prediction request for user {UserId}.", request.UserId);

        var prediction = await _aiPredictionService.PredictAsync(request, cancellationToken);
        return Ok(prediction);
    }
}

