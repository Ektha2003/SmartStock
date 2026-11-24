using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class AIEmailReplyProcessor : IAIEmailReplyProcessor
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIEmailReplyProcessor> _logger;
    private readonly IAIEmailGeneratorService _emailGeneratorService;
    private readonly IEmailService _emailService;

    public AIEmailReplyProcessor(
        ApplicationDbContext context,
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<AIEmailReplyProcessor> logger,
        IAIEmailGeneratorService emailGeneratorService,
        IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
        _emailGeneratorService = emailGeneratorService;
        _emailService = emailService;
    }

    public async Task<ApiResponse<AIEmailProcessingResultDto>> ProcessReplyAsync(
        AIEmailReplyRequestDto reply,
        CancellationToken cancellationToken = default)
    {
        var senderEmail = reply.FromEmail?.Trim().ToLowerInvariant() ?? string.Empty;
        var subject = reply.Subject?.Trim() ?? string.Empty;
        var body = reply.Body?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            return ApiResponse<AIEmailProcessingResultDto>.Fail("Sender email cannot be empty.");
        }

        // 1. Prevent duplicate email processing (Idempotency)
        if (!string.IsNullOrWhiteSpace(reply.MessageId))
        {
            var alreadyProcessed = await _context.CustomerEmailLogs
                .AnyAsync(l => l.MessageId == reply.MessageId && l.Direction == EmailDirection.Inbound, cancellationToken);
            if (alreadyProcessed)
            {
                return ApiResponse<AIEmailProcessingResultDto>.Fail("This message has already been processed.");
            }
        }

        // 2. Identify and validate customer by registered email
        var customer = await _context.Customers
            .Include(c => c.RecurringSetting)
            .Include(c => c.RecurringProducts)
                .ThenInclude(rp => rp.Product)
            .FirstOrDefaultAsync(c => c.Email.ToLower() == senderEmail && c.IsActive, cancellationToken);

        if (customer == null)
        {
            _logger.LogWarning("Email received from unregistered or inactive email: {SenderEmail}", senderEmail);
            return ApiResponse<AIEmailProcessingResultDto>.Fail($"Customer with registered email '{senderEmail}' not found or inactive.");
        }

        var businessId = customer.BusinessId;

        // 3. Locate active or pending recurring order for this customer
        var order = await _context.RecurringOrders
            .Include(ro => ro.Items)
                .ThenInclude(i => i.Product)
            .Where(ro => ro.CustomerId == customer.Id &&
                         ro.BusinessId == businessId &&
                         (ro.Status == RecurringOrderStatus.PendingCustomerApproval ||
                          ro.Status == RecurringOrderStatus.Draft ||
                          ro.Status == RecurringOrderStatus.ModificationRequested))
            .OrderByDescending(ro => ro.ScheduledOrderDate)
            .FirstOrDefaultAsync(cancellationToken);

        // Record Inbound Email Log
        var emailLog = new CustomerEmailLog
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CustomerId = customer.Id,
            RecurringOrderId = order?.Id,
            Direction = EmailDirection.Inbound,
            Subject = subject,
            Body = body,
            FromEmail = senderEmail,
            ToEmail = "orders@smartstock.local",
            MessageId = reply.MessageId ?? $"<inbound-{Guid.NewGuid():N}@smartstock.local>",
            ThreadId = reply.ThreadId ?? reply.MessageId,
            Status = EmailStatus.Received,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.CustomerEmailLogs.Add(emailLog);

        if (order == null)
        {
            var isSubChange = body.ToLowerInvariant().Contains("for all future") ||
                              body.ToLowerInvariant().Contains("future orders") ||
                              body.ToLowerInvariant().Contains("permanent");

            var generalProcessingLog = new AIEmailProcessingLog
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CustomerId = customer.Id,
                EmailLogId = emailLog.Id,
                RawEmailText = body,
                ParsedIntent = isSubChange ? "CHANGE_RECURRING_PREFERENCES" : "GENERAL_QUERY",
                ExtractedDataJson = "{}",
                ConfidenceScore = 0.95m,
                IsActionExecuted = false,
                RequiresManualReview = true,
                ManualReviewReason = isSubChange
                    ? "Customer requested permanent recurring subscription change. Requires explicit review & confirmation."
                    : "No pending recurring order found for customer.",
                ExecutionSummary = isSubChange
                    ? "Flagged for recurring subscription preference adjustment."
                    : "Logged customer query for merchant review.",
                CreatedAt = DateTime.UtcNow
            };
            _context.AIEmailProcessingLogs.Add(generalProcessingLog);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<AIEmailProcessingResultDto>.Ok(new AIEmailProcessingResultDto
            {
                Success = true,
                Intent = isSubChange ? "CHANGE_RECURRING_PREFERENCES" : "GENERAL_QUERY",
                Confidence = 0.95m,
                SubscriptionChange = isSubChange,
                RequiresConfirmation = isSubChange,
                RequiresManualReview = true,
                ManualReviewReason = generalProcessingLog.ManualReviewReason,
                ActionTaken = isSubChange ? "Subscription change flagged for review." : "Recorded query for merchant review.",
                CustomerMessage = isSubChange
                    ? "We noted your request to change your permanent recurring subscription. Our store team will update and confirm your recurring preferences."
                    : "We received your message. Our store manager will review it and get back to you shortly."
            });
        }

        // 4. Fetch available catalog for matching & stock validation
        var businessProducts = await _context.Products
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        // 5. AI Parsing: Parse reply via Gemini or Deterministic NLP parser
        var (intent, extractedItems, requestedDeliveryDate, subscriptionChange, confidence, rawAiResponse) =
            await ParseCustomerReplyWithAiAsync(body, order, customer, businessProducts, cancellationToken);

        var processingLog = new AIEmailProcessingLog
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CustomerId = customer.Id,
            RecurringOrderId = order.Id,
            EmailLogId = emailLog.Id,
            RawEmailText = body,
            ParsedIntent = intent,
            ExtractedDataJson = JsonSerializer.Serialize(new
            {
                intent,
                items = extractedItems,
                requestedDeliveryDate,
                subscriptionChange,
                confidence,
                rawAiResponse
            }),
            ConfidenceScore = confidence,
            CreatedAt = DateTime.UtcNow
        };

        var resultDto = new AIEmailProcessingResultDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Intent = intent,
            Confidence = confidence,
            SubscriptionChange = subscriptionChange
        };

        // SAFETY CHECK 1: Low Confidence -> Flag for Manual Review
        if (confidence < 0.85m || intent == "UNKNOWN")
        {
            processingLog.RequiresManualReview = true;
            processingLog.ManualReviewReason = $"AI low confidence score ({confidence:P0}). Requires staff verification.";
            processingLog.ExecutionSummary = "Order left unchanged pending merchant review.";

            order.RequiresManualReview = true;
            order.ManualReviewReason = processingLog.ManualReviewReason;
            order.LastCustomerResponseAt = DateTime.UtcNow;

            _context.AIEmailProcessingLogs.Add(processingLog);
            await _context.SaveChangesAsync(cancellationToken);

            resultDto.Success = true;
            resultDto.RequiresManualReview = true;
            resultDto.ManualReviewReason = processingLog.ManualReviewReason;
            resultDto.ActionTaken = "Flagged for manual review due to low AI confidence.";
            resultDto.CustomerMessage = "We have received your response and forwarded it to our team for quick verification.";
            return ApiResponse<AIEmailProcessingResultDto>.Ok(resultDto, "Flagged for manual review.");
        }

        // SAFETY CHECK 2: Permanent Subscription Change Request
        if (subscriptionChange || intent == "CHANGE_RECURRING_PREFERENCES")
        {
            processingLog.RequiresManualReview = true;
            processingLog.ManualReviewReason = "Customer requested permanent subscription/recurring preferences change. Requires explicit review & confirmation.";
            processingLog.ExecutionSummary = "Flagged for recurring subscription preference adjustment.";

            order.RequiresManualReview = true;
            order.ManualReviewReason = processingLog.ManualReviewReason;
            order.LastCustomerResponseAt = DateTime.UtcNow;

            _context.AIEmailProcessingLogs.Add(processingLog);
            await _context.SaveChangesAsync(cancellationToken);

            resultDto.Success = true;
            resultDto.SubscriptionChange = true;
            resultDto.RequiresConfirmation = true;
            resultDto.RequiresManualReview = true;
            resultDto.ManualReviewReason = processingLog.ManualReviewReason;
            resultDto.ActionTaken = "Subscription change flagged for review.";
            resultDto.CustomerMessage = "We noted your request to change your permanent recurring subscription. Our store team will update and confirm your recurring preferences.";
            return ApiResponse<AIEmailProcessingResultDto>.Ok(resultDto, "Subscription change flagged for confirmation.");
        }

        // 6. Execute Validated Business Actions
        try
        {
            switch (intent)
            {
                case "APPROVE_ORDER":
                    order.Status = RecurringOrderStatus.Approved;
                    order.CustomerApprovedAt = DateTime.UtcNow;
                    order.LastCustomerResponseAt = DateTime.UtcNow;
                    order.CustomerNotes = "Approved via email reply.";
                    order.RequiresManualReview = false;

                    processingLog.IsActionExecuted = true;
                    processingLog.ExecutionSummary = $"Approved order #{order.OrderNumber} as-is.";
                    resultDto.ActionTaken = "Order approved successfully.";
                    resultDto.CustomerMessage = $"Thank you! Order #{order.OrderNumber} is confirmed for delivery on {order.ExpectedDeliveryDate:dd MMMM yyyy}.";

                    // Send confirmation email
                    var confirmEmailBody = await _emailGeneratorService.GenerateOrderConfirmationEmailAsync(order, customer, cancellationToken);
                    await _emailService.SendEmailAsync(customer.Email, $"Order Confirmed - #{order.OrderNumber}", confirmEmailBody, businessId, customer.Id, order.Id, emailLog.MessageId, cancellationToken);
                    break;

                case "SKIP_ORDER":
                    order.Status = RecurringOrderStatus.Skipped;
                    order.LastCustomerResponseAt = DateTime.UtcNow;
                    order.CustomerNotes = "Skipped by customer via email reply.";

                    processingLog.IsActionExecuted = true;
                    processingLog.ExecutionSummary = $"Skipped order #{order.OrderNumber} for this cycle.";
                    resultDto.ActionTaken = "Order skipped for this cycle.";
                    resultDto.CustomerMessage = $"Understood. Your delivery scheduled for {order.ExpectedDeliveryDate:dd MMMM yyyy} has been skipped.";

                    var skipEmailBody = await _emailGeneratorService.GenerateOrderSkippedEmailAsync(order, customer, cancellationToken);
                    await _emailService.SendEmailAsync(customer.Email, $"Delivery Skipped - Order #{order.OrderNumber}", skipEmailBody, businessId, customer.Id, order.Id, emailLog.MessageId, cancellationToken);
                    break;

                case "RESCHEDULE_ORDER":
                    if (requestedDeliveryDate.HasValue)
                    {
                        var newDate = DateTime.SpecifyKind(requestedDeliveryDate.Value, DateTimeKind.Utc);
                        order.ExpectedDeliveryDate = newDate;
                        order.LastCustomerResponseAt = DateTime.UtcNow;
                        order.CustomerNotes = $"Rescheduled via email to {newDate:dd/MM/yyyy}.";

                        processingLog.IsActionExecuted = true;
                        processingLog.ExecutionSummary = $"Rescheduled delivery date to {newDate:dd MMMM yyyy}.";
                        resultDto.ActionTaken = $"Delivery rescheduled to {newDate:dd MMMM yyyy}.";
                        resultDto.CustomerMessage = $"Your delivery date has been rescheduled to {newDate:dd MMMM yyyy}.";
                    }
                    else
                    {
                        order.RequiresManualReview = true;
                        order.ManualReviewReason = "Customer requested reschedule without a specific date.";
                        processingLog.RequiresManualReview = true;
                        processingLog.ManualReviewReason = order.ManualReviewReason;
                        resultDto.ActionTaken = "Reschedule request flagged for staff assistance.";
                        resultDto.CustomerMessage = "Please let us know the exact date you'd prefer your delivery.";
                    }
                    break;

                case "MODIFY_ORDER":
                case "ADD_PRODUCT":
                case "REMOVE_PRODUCT":
                    // SAFETY CHECK 3: Verify all items against inventory & stock availability
                    var validationErrors = new List<string>();
                    var updatedItems = new List<RecurringOrderItem>(order.Items);

                    foreach (var extractedItem in extractedItems)
                    {
                        Product? targetProduct = null;
                        if (extractedItem.ProductId.HasValue)
                        {
                            targetProduct = businessProducts.FirstOrDefault(p => p.Id == extractedItem.ProductId.Value);
                        }
                        else if (!string.IsNullOrWhiteSpace(extractedItem.ProductName))
                        {
                            var targetName = extractedItem.ProductName.Trim().ToLowerInvariant();
                            targetProduct = businessProducts.FirstOrDefault(p => p.Name.ToLowerInvariant().Contains(targetName) || targetName.Contains(p.Name.ToLowerInvariant()));
                        }

                        if (targetProduct == null)
                        {
                            validationErrors.Add($"Product '{extractedItem.ProductName}' is not recognized in store inventory.");
                            continue;
                        }

                        extractedItem.ProductId = targetProduct.Id;
                        extractedItem.ProductName = targetProduct.Name;

                        var existingItem = updatedItems.FirstOrDefault(i => i.ProductId == targetProduct.Id);

                        if (extractedItem.Action == "REMOVE")
                        {
                            if (existingItem != null)
                            {
                                updatedItems.Remove(existingItem);
                                _context.RecurringOrderItems.Remove(existingItem);
                            }
                        }
                        else if (extractedItem.Action == "UPDATE" || extractedItem.Action == "ADD")
                        {
                            if (extractedItem.Quantity <= 0)
                            {
                                validationErrors.Add($"Invalid quantity {extractedItem.Quantity} for '{targetProduct.Name}'.");
                                continue;
                            }

                            if (targetProduct.CurrentStock < extractedItem.Quantity)
                            {
                                validationErrors.Add($"Insufficient stock for '{targetProduct.Name}' (requested {extractedItem.Quantity}, available {targetProduct.CurrentStock}).");
                                continue;
                            }

                            if (existingItem != null)
                            {
                                existingItem.Quantity = extractedItem.Quantity;
                                existingItem.IsModifiedByCustomer = true;
                                existingItem.UnitPrice = targetProduct.SellingPrice;
                                existingItem.SubTotal = Math.Round(existingItem.Quantity * existingItem.UnitPrice, 2);
                            }
                            else
                            {
                                var newItem = new RecurringOrderItem
                                {
                                    Id = Guid.NewGuid(),
                                    RecurringOrderId = order.Id,
                                    ProductId = targetProduct.Id,
                                    Quantity = extractedItem.Quantity,
                                    Unit = targetProduct.Unit,
                                    UnitPrice = targetProduct.SellingPrice,
                                    SubTotal = Math.Round(extractedItem.Quantity * targetProduct.SellingPrice, 2),
                                    IsAddedByCustomer = true,
                                    Product = targetProduct
                                };
                                updatedItems.Add(newItem);
                                _context.RecurringOrderItems.Add(newItem);
                            }
                        }
                    }

                    if (validationErrors.Count > 0)
                    {
                        processingLog.RequiresManualReview = true;
                        processingLog.ValidationErrors = string.Join("; ", validationErrors);
                        processingLog.ManualReviewReason = "Inventory validation issues during order modification.";
                        order.RequiresManualReview = true;
                        order.ManualReviewReason = processingLog.ValidationErrors;

                        resultDto.RequiresManualReview = true;
                        resultDto.ManualReviewReason = processingLog.ValidationErrors;
                        resultDto.ActionTaken = "Modification flagged for review due to inventory validation issues.";
                        resultDto.CustomerMessage = $"We received your update request, but some items require verification: {string.Join(", ", validationErrors)}. Our team is assisting you.";
                    }
                    else
                    {
                        order.Items = updatedItems;
                        order.SubTotal = updatedItems.Sum(i => i.SubTotal);
                        order.TotalAmount = order.SubTotal;
                        order.Status = RecurringOrderStatus.Approved;
                        order.CustomerApprovedAt = DateTime.UtcNow;
                        order.LastCustomerResponseAt = DateTime.UtcNow;
                        order.CustomerNotes = "Modified and approved via email reply.";

                        processingLog.IsActionExecuted = true;
                        processingLog.ExecutionSummary = $"Updated items and approved order #{order.OrderNumber}. New total: ₹{order.TotalAmount:N2}.";

                        resultDto.ActionTaken = "Items updated and order approved.";
                        resultDto.CustomerMessage = $"Your order #{order.OrderNumber} has been updated and confirmed! New Total: ₹{order.TotalAmount:N2}. Expected Delivery: {order.ExpectedDeliveryDate:dd MMMM yyyy}.";

                        var updatedEmailBody = await _emailGeneratorService.GenerateOrderConfirmationEmailAsync(order, customer, cancellationToken);
                        await _emailService.SendEmailAsync(customer.Email, $"Updated Order Confirmed - #{order.OrderNumber}", updatedEmailBody, businessId, customer.Id, order.Id, emailLog.MessageId, cancellationToken);
                    }
                    break;

                default:
                    order.RequiresManualReview = true;
                    order.ManualReviewReason = $"Unrecognized intent: {intent}";
                    processingLog.RequiresManualReview = true;
                    processingLog.ManualReviewReason = order.ManualReviewReason;
                    resultDto.ActionTaken = "Flagged for manual review.";
                    resultDto.CustomerMessage = "Thank you for your message. Our team will review your request and get back to you.";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing natural language email action for order {OrderId}", order.Id);
            processingLog.RequiresManualReview = true;
            processingLog.ManualReviewReason = $"Execution error: {ex.Message}";
            order.RequiresManualReview = true;
            order.ManualReviewReason = "System execution error during email processing.";

            resultDto.RequiresManualReview = true;
            resultDto.ManualReviewReason = ex.Message;
            resultDto.ActionTaken = "Error occurred. Flagged for manual handling.";
            resultDto.CustomerMessage = "We encountered a temporary issue processing your update. Our team has been notified.";
        }

        order.UpdatedAt = DateTime.UtcNow;
        resultDto.Success = true;
        resultDto.Items = extractedItems;

        _context.AIEmailProcessingLogs.Add(processingLog);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<AIEmailProcessingResultDto>.Ok(resultDto, "Email reply processed successfully.");
    }

    private async Task<(string Intent, List<AIExtractedItemDto> Items, DateTime? DeliveryDate, bool SubscriptionChange, decimal Confidence, string RawResponse)>
        ParseCustomerReplyWithAiAsync(
            string emailBody,
            RecurringOrder order,
            Customer customer,
            List<Product> products,
            CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-3.8-flash";

        if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith("your_"))
        {
            try
            {
                var productCatalogCatalog = string.Join("\n", products.Take(40).Select(p => $"- ID: {p.Id}, Name: {p.Name}, Unit: {p.Unit}, AvailableStock: {p.CurrentStock}"));
                var currentItems = string.Join("\n", order.Items.Select(i => $"- Product: {i.Product?.Name}, CurrentQty: {i.Quantity} {i.Unit}"));

                var prompt = $@"You are SmartStock AI Classifier for retail customer emails.
Analyze the following customer email reply regarding their recurring grocery delivery order.
CURRENT ORDER ITEMS:
{currentItems}

AVAILABLE STORE CATALOG:
{productCatalogCatalog}

CUSTOMER EMAIL REPLY:
""{emailBody}""

Extract the intent and structured modifications.
Output ONLY a JSON object matching this schema:
{{
  ""intent"": ""APPROVE_ORDER"" | ""MODIFY_ORDER"" | ""ADD_PRODUCT"" | ""REMOVE_PRODUCT"" | ""SKIP_ORDER"" | ""RESCHEDULE_ORDER"" | ""CHANGE_RECURRING_PREFERENCES"" | ""GENERAL_QUERY"" | ""UNKNOWN"",
  ""confidence"": 0.95,
  ""subscriptionChange"": false,
  ""deliveryDate"": ""YYYY-MM-DD"" or null,
  ""items"": [
    {{
      ""productId"": ""UUID"" or null,
      ""productName"": ""string"",
      ""quantity"": 10,
      ""unit"": ""string"",
      ""action"": ""UPDATE"" | ""ADD"" | ""REMOVE""
    }}
  ],
  ""customerMessage"": ""Brief summary""
}}";

                var requestBody = new
                {
                    contents = new[] { new { role = "user", parts = new object[] { new { text = prompt } } } },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        responseMimeType = "application/json"
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}",
                    content,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var doc = JsonNode.Parse(responseJson);
                    var rawText = doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "{}";

                    using var parsedDoc = JsonDocument.Parse(rawText);
                    var root = parsedDoc.RootElement;

                    var intent = root.GetProperty("intent").GetString() ?? "UNKNOWN";
                    var confidence = root.TryGetProperty("confidence", out var confProp) ? confProp.GetDecimal() : 0.90m;
                    var subChange = root.TryGetProperty("subscriptionChange", out var subProp) && subProp.GetBoolean();

                    DateTime? deliveryDate = null;
                    if (root.TryGetProperty("deliveryDate", out var dateProp) && !string.IsNullOrWhiteSpace(dateProp.GetString()))
                    {
                        if (DateTime.TryParse(dateProp.GetString(), out var parsedDate))
                        {
                            deliveryDate = parsedDate;
                        }
                    }

                    var itemsList = new List<AIExtractedItemDto>();
                    if (root.TryGetProperty("items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var it in itemsArray.EnumerateArray())
                        {
                            Guid? pId = null;
                            if (it.TryGetProperty("productId", out var pidProp) && Guid.TryParse(pidProp.GetString(), out var guidVal))
                            {
                                pId = guidVal;
                            }

                            itemsList.Add(new AIExtractedItemDto
                            {
                                ProductId = pId,
                                ProductName = it.TryGetProperty("productName", out var nameProp) ? nameProp.GetString() : null,
                                Quantity = it.TryGetProperty("quantity", out var qtyProp) ? qtyProp.GetDecimal() : 0,
                                Unit = it.TryGetProperty("unit", out var unitProp) ? unitProp.GetString() : null,
                                Action = it.TryGetProperty("action", out var actProp) ? actProp.GetString() ?? "UPDATE" : "UPDATE"
                            });
                        }
                    }

                    return (intent, itemsList, deliveryDate, subChange, confidence, rawText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini parsing failed. Falling back to deterministic NLP parser.");
            }
        }

        // Deterministic Fallback NLP Classifier
        return DeterministicNlpParse(emailBody, order, products);
    }

    private static (string Intent, List<AIExtractedItemDto> Items, DateTime? DeliveryDate, bool SubscriptionChange, decimal Confidence, string RawResponse)
        DeterministicNlpParse(string emailBody, RecurringOrder order, List<Product> products)
    {
        var text = emailBody.Trim().ToLowerInvariant();

        // 1. Permanent subscription change
        if (text.Contains("for all future") || text.Contains("future orders") || text.Contains("permanent") || text.Contains("every time from now"))
        {
            return ("CHANGE_RECURRING_PREFERENCES", new List<AIExtractedItemDto>(), null, true, 0.95m, "Deterministic rule matched: permanent subscription change request.");
        }

        // 2. Skip order
        if (text.Contains("don't send") || text.Contains("dont send") || text.Contains("skip") || text.Contains("cancel this") || text.Contains("not this time") || text.Contains("nothing this time"))
        {
            return ("SKIP_ORDER", new List<AIExtractedItemDto>(), null, false, 0.98m, "Deterministic rule matched: skip order.");
        }

        // 3. Approve order
        if (text.Contains("yes") || text.Contains("send me the same") || text.Contains("confirm") || text.Contains("approve") || text.Contains("same order") || text.Contains("send it") || text.Contains("ok send"))
        {
            // If they didn't ask to modify items or reschedule
            if (!text.Contains("instead") && !text.Contains("remove") && !text.Contains("add") && !text.Contains("next"))
            {
                return ("APPROVE_ORDER", new List<AIExtractedItemDto>(), null, false, 0.98m, "Deterministic rule matched: approve order.");
            }
        }

        // 4. Reschedule
        if (text.Contains("next monday") || text.Contains("reschedule") || text.Contains("tomorrow") || text.Contains("next week") || text.Contains("send it on"))
        {
            DateTime? targetDate = null;
            if (text.Contains("next monday"))
            {
                var today = DateTime.UtcNow;
                int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
                if (daysUntilMonday == 0) daysUntilMonday = 7;
                targetDate = today.AddDays(daysUntilMonday);
            }
            else if (text.Contains("tomorrow"))
            {
                targetDate = DateTime.UtcNow.AddDays(1);
            }

            return ("RESCHEDULE_ORDER", new List<AIExtractedItemDto>(), targetDate, false, 0.92m, "Deterministic rule matched: reschedule order.");
        }

        // 5. Modify order (Quantities, Add, Remove)
        var items = new List<AIExtractedItemDto>();

        // Regex for updates like "15 kg rice" or "3 litres oil"
        var matches = Regex.Matches(text, @"(\d+(\.\d+)?)\s*(kg|kilo|litre|litres|liter|liters|gm|gram|packet|box|pieces?|pcs?)?\s+([a-zA-Z\s]+)");
        foreach (Match m in matches)
        {
            if (decimal.TryParse(m.Groups[1].Value, out var qty))
            {
                var rawUnit = m.Groups[3].Value;
                var rawName = m.Groups[4].Value.Trim();

                // Match with product
                var matched = products.FirstOrDefault(p => rawName.Contains(p.Name.ToLowerInvariant()) || p.Name.ToLowerInvariant().Contains(rawName));
                if (matched != null)
                {
                    items.Add(new AIExtractedItemDto
                    {
                        ProductId = matched.Id,
                        ProductName = matched.Name,
                        Quantity = qty,
                        Unit = rawUnit,
                        Action = "UPDATE"
                    });
                }
            }
        }

        // Check for "remove sugar" or "remove X"
        var removeMatches = Regex.Matches(text, @"remove\s+([a-zA-Z]+)");
        foreach (Match m in removeMatches)
        {
            var rawName = m.Groups[1].Value.Trim();
            var matched = products.FirstOrDefault(p => p.Name.ToLowerInvariant().Contains(rawName));
            if (matched != null)
            {
                items.Add(new AIExtractedItemDto
                {
                    ProductId = matched.Id,
                    ProductName = matched.Name,
                    Quantity = 0,
                    Action = "REMOVE"
                });
            }
        }

        if (items.Count > 0)
        {
            return ("MODIFY_ORDER", items, null, false, 0.94m, "Deterministic rule matched: modify order quantities.");
        }

        return ("UNKNOWN", new List<AIExtractedItemDto>(), null, false, 0.60m, "Unable to extract intent with certainty.");
    }
}
