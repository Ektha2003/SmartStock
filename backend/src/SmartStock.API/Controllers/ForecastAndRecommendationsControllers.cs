using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class ForecastController : BaseApiController
{
    private readonly IForecastingService _forecastingService;

    public ForecastController(IForecastingService forecastingService)
    {
        _forecastingService = forecastingService;
    }

    /// <summary>
    /// Retrieve rule-based demand forecasts, velocity, and runout projections for all active products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ForecastDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForecasts(CancellationToken cancellationToken)
    {
        var result = await _forecastingService.GetForecastsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve demand forecast metrics for a specific product.
    /// </summary>
    [HttpGet("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ForecastDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductForecast(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _forecastingService.GetProductForecastAsync(productId, cancellationToken);
        return Ok(result);
    }
}

[Authorize]
[Route("api/v1/[controller]")]
public class RecommendationsController : BaseApiController
{
    private readonly IForecastingService _forecastingService;

    public RecommendationsController(IForecastingService forecastingService)
    {
        _forecastingService = forecastingService;
    }

    /// <summary>
    /// Retrieve purchase order recommendations with suggested order quantities and estimated costs.
    /// </summary>
    [HttpGet("purchases")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PurchaseRecommendationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseRecommendations(CancellationToken cancellationToken)
    {
        var result = await _forecastingService.GetPurchaseRecommendationsAsync(cancellationToken);
        return Ok(result);
    }
}
