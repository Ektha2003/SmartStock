using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class BusinessController : BaseApiController
{
    private readonly IBusinessService _businessService;

    public BusinessController(IBusinessService businessService)
    {
        _businessService = businessService;
    }

    /// <summary>
    /// Retrieve the current authenticated user's isolated business details.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<BusinessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBusiness(CancellationToken cancellationToken)
    {
        var result = await _businessService.GetCurrentBusinessAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update current store information.
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<BusinessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBusiness([FromBody] UpdateBusinessDto dto, CancellationToken cancellationToken)
    {
        var result = await _businessService.UpdateBusinessAsync(dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Post/Confirm business profile configuration.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<BusinessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrConfigureBusiness([FromBody] UpdateBusinessDto dto, CancellationToken cancellationToken)
    {
        var result = await _businessService.UpdateBusinessAsync(dto, cancellationToken);
        return Ok(result);
    }
}
