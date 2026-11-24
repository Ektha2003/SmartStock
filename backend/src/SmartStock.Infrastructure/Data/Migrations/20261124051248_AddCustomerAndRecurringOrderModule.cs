using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAndRecurringOrderModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderSource",
                table: "sales",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BillingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeliveryAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreferredContactMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecurringOrderEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DefaultDeliveryInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customers_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_recurring_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriceOverride = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MinQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    MaxQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    ProductInstructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_recurring_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_recurring_products_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_recurring_products_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_recurring_products_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_recurring_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrequencyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IntervalDays = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextOrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreferredDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreferredDeliveryTime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AutoEmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RequireCustomerApproval = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderBeforeDeliveryDays = table.Column<int>(type: "integer", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Asia/Kolkata"),
                    LastOrderGeneratedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubscriptionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_recurring_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_recurring_settings_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_recurring_settings_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recurring_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrderSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledOrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreferredDeliveryTime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomerNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SpecialInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovalRequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCustomerResponseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedSaleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequiresManualReview = table.Column<bool>(type: "boolean", nullable: false),
                    ManualReviewReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_orders_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_orders_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_orders_sales_ConfirmedSaleId",
                        column: x => x.ConfirmedSaleId,
                        principalTable: "sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customer_email_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    FromEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ToEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ThreadId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_email_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_email_logs_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_email_logs_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_email_logs_recurring_orders_RecurringOrderId",
                        column: x => x.RecurringOrderId,
                        principalTable: "recurring_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "recurring_order_approval_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClientIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_order_approval_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_order_approval_tokens_recurring_orders_RecurringO~",
                        column: x => x.RecurringOrderId,
                        principalTable: "recurring_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recurring_order_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProductInstructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsAddedByCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    IsModifiedByCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_order_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_order_items_recurring_orders_RecurringOrderId",
                        column: x => x.RecurringOrderId,
                        principalTable: "recurring_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_email_processing_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmailLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    RawEmailText = table.Column<string>(type: "text", nullable: false),
                    ParsedIntent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExtractedDataJson = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    IsActionExecuted = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresManualReview = table.Column<bool>(type: "boolean", nullable: false),
                    ManualReviewReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValidationErrors = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExecutionSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_email_processing_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_email_processing_logs_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_email_processing_logs_customer_email_logs_EmailLogId",
                        column: x => x.EmailLogId,
                        principalTable: "customer_email_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ai_email_processing_logs_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_email_processing_logs_recurring_orders_RecurringOrderId",
                        column: x => x.RecurringOrderId,
                        principalTable: "recurring_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_CustomerId",
                table: "sales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_email_processing_logs_BusinessId",
                table: "ai_email_processing_logs",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_email_processing_logs_CreatedAt",
                table: "ai_email_processing_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_email_processing_logs_CustomerId",
                table: "ai_email_processing_logs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_email_processing_logs_EmailLogId",
                table: "ai_email_processing_logs",
                column: "EmailLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_email_processing_logs_RecurringOrderId",
                table: "ai_email_processing_logs",
                column: "RecurringOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_email_processing_logs_RequiresManualReview",
                table: "ai_email_processing_logs",
                column: "RequiresManualReview");

            migrationBuilder.CreateIndex(
                name: "IX_customer_email_logs_BusinessId",
                table: "customer_email_logs",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_email_logs_CreatedAt",
                table: "customer_email_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_customer_email_logs_CustomerId",
                table: "customer_email_logs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_email_logs_MessageId",
                table: "customer_email_logs",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_email_logs_RecurringOrderId",
                table: "customer_email_logs",
                column: "RecurringOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_email_logs_ThreadId",
                table: "customer_email_logs",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_recurring_products_BusinessId",
                table: "customer_recurring_products",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_recurring_products_CustomerId_ProductId",
                table: "customer_recurring_products",
                columns: new[] { "CustomerId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_recurring_products_ProductId",
                table: "customer_recurring_products",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_recurring_settings_BusinessId",
                table: "customer_recurring_settings",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_recurring_settings_CustomerId",
                table: "customer_recurring_settings",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_recurring_settings_NextOrderDate",
                table: "customer_recurring_settings",
                column: "NextOrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_customer_recurring_settings_SubscriptionStatus",
                table: "customer_recurring_settings",
                column: "SubscriptionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_customers_BusinessId",
                table: "customers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_BusinessId_Email",
                table: "customers",
                columns: new[] { "BusinessId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_CreatedAt",
                table: "customers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Name",
                table: "customers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Phone",
                table: "customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_order_approval_tokens_ExpiresAt",
                table: "recurring_order_approval_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_order_approval_tokens_RecurringOrderId",
                table: "recurring_order_approval_tokens",
                column: "RecurringOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_order_approval_tokens_Token",
                table: "recurring_order_approval_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_order_items_ProductId",
                table: "recurring_order_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_order_items_RecurringOrderId",
                table: "recurring_order_items",
                column: "RecurringOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_orders_BusinessId",
                table: "recurring_orders",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_orders_ConfirmedSaleId",
                table: "recurring_orders",
                column: "ConfirmedSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_orders_CustomerId",
                table: "recurring_orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_orders_ExpectedDeliveryDate",
                table: "recurring_orders",
                column: "ExpectedDeliveryDate");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_orders_OrderNumber",
                table: "recurring_orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_orders_RequiresManualReview",
                table: "recurring_orders",
                column: "RequiresManualReview");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_orders_ScheduledOrderDate",
                table: "recurring_orders",
                column: "ScheduledOrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_orders_Status",
                table: "recurring_orders",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_sales_customers_CustomerId",
                table: "sales",
                column: "CustomerId",
                principalTable: "customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sales_customers_CustomerId",
                table: "sales");

            migrationBuilder.DropTable(
                name: "ai_email_processing_logs");

            migrationBuilder.DropTable(
                name: "customer_recurring_products");

            migrationBuilder.DropTable(
                name: "customer_recurring_settings");

            migrationBuilder.DropTable(
                name: "recurring_order_approval_tokens");

            migrationBuilder.DropTable(
                name: "recurring_order_items");

            migrationBuilder.DropTable(
                name: "customer_email_logs");

            migrationBuilder.DropTable(
                name: "recurring_orders");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropIndex(
                name: "IX_sales_CustomerId",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "OrderSource",
                table: "sales");
        }
    }
}
