using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierInvoiceManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                table: "suppliers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "suppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GstNumber",
                table: "suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "suppliers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PanNumber",
                table: "suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "supplier_invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierNameRaw = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SupplierGst = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BillingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveryAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PurchaseOrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Cgst = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Sgst = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Igst = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherCharges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DocumentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDuplicate = table.Column<bool>(type: "boolean", nullable: false),
                    DuplicateReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExtractedRawJson = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByUserName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ConfirmedPurchaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_invoices_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_invoices_purchases_ConfirmedPurchaseId",
                        column: x => x.ConfirmedPurchaseId,
                        principalTable: "purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_supplier_invoices_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ai_invoice_processing_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ExtractedDataJson = table.Column<string>(type: "text", nullable: true),
                    ExecutionSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Errors = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_invoice_processing_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_invoice_processing_logs_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_invoice_processing_logs_supplier_invoices_SupplierInvoic~",
                        column: x => x.SupplierInvoiceId,
                        principalTable: "supplier_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_purchase_price_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NewPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_purchase_price_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_purchase_price_histories_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_purchase_price_histories_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_purchase_price_histories_supplier_invoices_Supplier~",
                        column: x => x.SupplierInvoiceId,
                        principalTable: "supplier_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_product_purchase_price_histories_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "supplier_invoice_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawProductName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    RawDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RawSupplierSku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RawHsnCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InvoiceQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    InvoiceUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MatchedProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MatchConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    MatchExplanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConversionFactor = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PackSize = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    CalculatedStockQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    TargetUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreviousPurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NewPurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceDifference = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentagePriceChange = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    ShouldUpdatePurchasePrice = table.Column<bool>(type: "boolean", nullable: false),
                    IsIgnored = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_invoice_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_invoice_items_products_MatchedProductId",
                        column: x => x.MatchedProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_supplier_invoice_items_supplier_invoices_SupplierInvoiceId",
                        column: x => x.SupplierInvoiceId,
                        principalTable: "supplier_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_GstNumber",
                table: "suppliers",
                column: "GstNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ai_invoice_processing_logs_BusinessId",
                table: "ai_invoice_processing_logs",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_invoice_processing_logs_CreatedAt",
                table: "ai_invoice_processing_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_invoice_processing_logs_Stage",
                table: "ai_invoice_processing_logs",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_ai_invoice_processing_logs_SupplierInvoiceId",
                table: "ai_invoice_processing_logs",
                column: "SupplierInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_product_purchase_price_histories_BusinessId",
                table: "product_purchase_price_histories",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_product_purchase_price_histories_EffectiveDate",
                table: "product_purchase_price_histories",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_product_purchase_price_histories_ProductId",
                table: "product_purchase_price_histories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_purchase_price_histories_SupplierId",
                table: "product_purchase_price_histories",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_product_purchase_price_histories_SupplierInvoiceId",
                table: "product_purchase_price_histories",
                column: "SupplierInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoice_items_MatchedProductId",
                table: "supplier_invoice_items",
                column: "MatchedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoice_items_MatchStatus",
                table: "supplier_invoice_items",
                column: "MatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoice_items_SupplierInvoiceId",
                table: "supplier_invoice_items",
                column: "SupplierInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_BusinessId",
                table: "supplier_invoices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_BusinessId_DocumentHash",
                table: "supplier_invoices",
                columns: new[] { "BusinessId", "DocumentHash" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_BusinessId_InvoiceNumber",
                table: "supplier_invoices",
                columns: new[] { "BusinessId", "InvoiceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_ConfirmedPurchaseId",
                table: "supplier_invoices",
                column: "ConfirmedPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_CreatedAt",
                table: "supplier_invoices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_InvoiceDate",
                table: "supplier_invoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_Status",
                table: "supplier_invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_SupplierId",
                table: "supplier_invoices",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_invoice_processing_logs");

            migrationBuilder.DropTable(
                name: "product_purchase_price_histories");

            migrationBuilder.DropTable(
                name: "supplier_invoice_items");

            migrationBuilder.DropTable(
                name: "supplier_invoices");

            migrationBuilder.DropIndex(
                name: "IX_suppliers_GstNumber",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "GstNumber",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "PanNumber",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "suppliers");
        }
    }
}
