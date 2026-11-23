using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class AIController : BaseApiController
{
    private readonly IAIAssistantService _aiAssistantService;
    private readonly IInvoiceScannerService _invoiceScannerService;

    public AIController(IAIAssistantService aiAssistantService, IInvoiceScannerService invoiceScannerService)
    {
        _aiAssistantService = aiAssistantService;
        _invoiceScannerService = invoiceScannerService;
    }

    /// <summary>
    /// Ask SmartStock AI business assistant about inventory, sales, stock levels, or reordering suggestions.
    /// </summary>
    [HttpPost("assistant")]
    [ProducesResponseType(typeof(ApiResponse<AIAssistantResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AskAssistant([FromBody] AIAssistantRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.AskAssistantAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Upload an invoice document/image to extract supplier, date, line items, and prices automatically.
    /// </summary>
    [HttpPost("invoice-extract")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceExtractResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtractInvoice(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("File is required.", "file", "Please upload a valid invoice image or PDF document."));
        }

        using var stream = file.OpenReadStream();
        var result = await _invoiceScannerService.ExtractInvoiceAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(result);
    }
}
