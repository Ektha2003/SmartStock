using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Route("api/v1/email-processing")]
public class EmailProcessingController : BaseApiController
{
    private readonly IAIEmailReplyProcessor _replyProcessor;
    private readonly IEmailService _emailService;

    public EmailProcessingController(
        IAIEmailReplyProcessor replyProcessor,
        IEmailService emailService)
    {
        _replyProcessor = replyProcessor;
        _emailService = emailService;
    }

    /// <summary>
    /// Process incoming customer natural language email reply.
    /// Can be called by Gmail Pub/Sub webhook, email inbound hooks, or admin testing console.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("simulate-reply")]
    [ProducesResponseType(typeof(ApiResponse<AIEmailProcessingResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessReply([FromBody] AIEmailReplyRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _replyProcessor.ProcessReplyAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize]
    [HttpGet("email-logs")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomerEmailLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmailLogs(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? orderId,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        var result = await _emailService.GetEmailLogsAsync(customerId, orderId, pagination, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("ai-logs")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AIEmailProcessingLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAILogs(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? orderId,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        var result = await _emailService.GetAILogsAsync(customerId, orderId, pagination, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("test-send")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendTestEmail([FromBody] AIEmailReplyRequestDto dto, CancellationToken cancellationToken)
    {
        var sent = await _emailService.SendEmailAsync(
            dto.FromEmail,
            dto.Subject,
            dto.Body,
            null,
            null,
            null,
            null,
            cancellationToken);

        return Ok(ApiResponse<bool>.Ok(sent, sent ? "Test email dispatched successfully." : "Failed to dispatch email. Check SMTP settings."));
    }
}
