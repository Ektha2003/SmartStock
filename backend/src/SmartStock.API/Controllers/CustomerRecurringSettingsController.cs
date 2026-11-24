using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Enums;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/customers/{customerId:guid}/recurring-settings")]
public class CustomerRecurringSettingsController : BaseApiController
{
    private readonly ICustomerRecurringSettingService _settingService;

    public CustomerRecurringSettingsController(ICustomerRecurringSettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CustomerRecurringSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSetting(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _settingService.GetSettingAsync(customerId, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<CustomerRecurringSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSetting(Guid customerId, [FromBody] UpdateCustomerRecurringSettingDto dto, CancellationToken cancellationToken)
    {
        var result = await _settingService.UpdateSettingAsync(customerId, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("status")]
    [ProducesResponseType(typeof(ApiResponse<CustomerRecurringSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleStatus(Guid customerId, [FromQuery] SubscriptionStatus status, CancellationToken cancellationToken)
    {
        var result = await _settingService.ToggleSubscriptionStatusAsync(customerId, status, cancellationToken);
        return Ok(result);
    }
}
