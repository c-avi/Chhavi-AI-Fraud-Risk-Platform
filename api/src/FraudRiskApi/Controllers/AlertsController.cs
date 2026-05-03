using FraudRiskApi.Models;
using FraudRiskApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FraudRiskApi.Controllers;

[ApiController]
[Route("api/v1/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> GetAlerts(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var alerts = await _alertService.GetAlertsAsync(limit, cancellationToken);
        return Ok(alerts);
    }
}
