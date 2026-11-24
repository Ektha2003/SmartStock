using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[AllowAnonymous]
[Route("api/v1/customer-portal")]
public class CustomerPortalController : BaseApiController
{
    private readonly ICustomerPortalService _portalService;

    public CustomerPortalController(ICustomerPortalService portalService)
    {
        _portalService = portalService;
    }

    /// <summary>
    /// Retrieve customer recurring order details for approval using a unique expiring token.
    /// No customer authentication required.
    /// </summary>
    [HttpGet("order-by-token/{token}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerPortalOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByToken(string token, CancellationToken cancellationToken)
    {
        var result = await _portalService.GetOrderByTokenAsync(token, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Customer submits their decision (Approve as-is, Modify quantities/items, or Skip delivery).
    /// </summary>
    [HttpPost("submit-order/{token}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerPortalOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitOrderDecision(string token, [FromBody] CustomerPortalSubmitDto dto, CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _portalService.SubmitOrderDecisionAsync(token, dto, clientIp, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
