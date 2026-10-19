using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITAssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAssetManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmployeeId);
                });

            migrationBuilder.CreateTable(
                name: "StockReceipts",
                columns: table => new
                {
                    ReceiptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Supplier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReceipts", x => x.ReceiptId);
                });

            migrationBuilder.CreateTable(
                name: "AssetProducts",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrackingType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinimumStock = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetProducts", x => x.ProductId);
                    table.CheckConstraint("CK_AssetProducts_MinimumStock", "[MinimumStock] >= 0");
                    table.CheckConstraint("CK_AssetProducts_TrackingType", "[TrackingType] IN ('Individual', 'Bulk')");
                    table.ForeignKey(
                        name: "FK_AssetProducts_AssetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AssetCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_AssetAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetUnits",
                columns: table => new
                {
                    AssetUnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchaseCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetUnits", x => x.AssetUnitId);
                    table.CheckConstraint("CK_AssetUnits_Condition", "[Condition] IN ('New', 'Good', 'Fair', 'Poor', 'Damaged')");
                    table.CheckConstraint("CK_AssetUnits_PurchaseCost", "[PurchaseCost] >= 0");
                    table.CheckConstraint("CK_AssetUnits_Status", "[Status] IN ('Available', 'Assigned', 'UnderRepair', 'Retired')");
                    table.ForeignKey(
                        name: "FK_AssetUnits_AssetProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AssetProducts",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                columns: table => new
                {
                    AdjustmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    QuantityChange = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.AdjustmentId);
                    table.CheckConstraint("CK_StockAdjustments_QuantityChange", "[QuantityChange] <> 0");
                    table.ForeignKey(
                        name: "FK_StockAdjustments_AssetProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AssetProducts",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockReceiptItems",
                columns: table => new
                {
                    ReceiptItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReceiptItems", x => x.ReceiptItemId);
                    table.CheckConstraint("CK_StockReceiptItems_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_StockReceiptItems_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_StockReceiptItems_AssetProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AssetProducts",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceiptItems_StockReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "StockReceipts",
                        principalColumn: "ReceiptId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetAssignmentItems",
                columns: table => new
                {
                    AssignmentItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AssetUnitId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignmentItems", x => x.AssignmentItemId);
                    table.CheckConstraint("CK_AssetAssignmentItems_IndividualQuantity", "[AssetUnitId] IS NULL OR [Quantity] = 1");
                    table.CheckConstraint("CK_AssetAssignmentItems_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_AssetAssignmentItems_AssetAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "AssetAssignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetAssignmentItems_AssetProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AssetProducts",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetAssignmentItems_AssetUnits_AssetUnitId",
                        column: x => x.AssetUnitId,
                        principalTable: "AssetUnits",
                        principalColumn: "AssetUnitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    MovementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ReceiptItemId = table.Column<int>(type: "int", nullable: true),
                    AssetUnitId = table.Column<int>(type: "int", nullable: true),
                    MovementType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.MovementId);
                    table.CheckConstraint("CK_StockMovements_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_StockMovements_AssetProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AssetProducts",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_AssetUnits_AssetUnitId",
                        column: x => x.AssetUnitId,
                        principalTable: "AssetUnits",
                        principalColumn: "AssetUnitId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_StockReceiptItems_ReceiptItemId",
                        column: x => x.ReceiptItemId,
                        principalTable: "StockReceiptItems",
                        principalColumn: "ReceiptItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetReturns",
                columns: table => new
                {
                    ReturnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentItemId = table.Column<int>(type: "int", nullable: false),
                    ReturnQuantity = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedCondition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetReturns", x => x.ReturnId);
                    table.CheckConstraint("CK_AssetReturns_ReturnQuantity", "[ReturnQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_AssetReturns_AssetAssignmentItems_AssignmentItemId",
                        column: x => x.AssignmentItemId,
                        principalTable: "AssetAssignmentItems",
                        principalColumn: "AssignmentItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentItems_AssetUnitId",
                table: "AssetAssignmentItems",
                column: "AssetUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentItems_AssignmentId",
                table: "AssetAssignmentItems",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentItems_ProductId",
                table: "AssetAssignmentItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_AssignedDate",
                table: "AssetAssignments",
                column: "AssignedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_EmployeeId",
                table: "AssetAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCategories_CategoryName",
                table: "AssetCategories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetProducts_CategoryId",
                table: "AssetProducts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReturns_AssignmentItemId",
                table: "AssetReturns",
                column: "AssignmentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetUnits_AssetCode",
                table: "AssetUnits",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetUnits_ProductId",
                table: "AssetUnits",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetUnits_SerialNumber",
                table: "AssetUnits",
                column: "SerialNumber",
                unique: true,
                filter: "[SerialNumber] IS NOT NULL AND [SerialNumber] <> N''");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_AdjustmentDate",
                table: "StockAdjustments",
                column: "AdjustmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ProductId",
                table: "StockAdjustments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_AssetUnitId",
                table: "StockMovements",
                column: "AssetUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementType_ReferenceId",
                table: "StockMovements",
                columns: new[] { "MovementType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReceiptItemId",
                table: "StockMovements",
                column: "ReceiptItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptItems_ProductId",
                table: "StockReceiptItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptItems_ReceiptId",
                table: "StockReceiptItems",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_ReceiptNumber",
                table: "StockReceipts",
                column: "ReceiptNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetReturns");

            migrationBuilder.DropTable(
                name: "StockAdjustments");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "AssetAssignmentItems");

            migrationBuilder.DropTable(
                name: "StockReceiptItems");

            migrationBuilder.DropTable(
                name: "AssetAssignments");

            migrationBuilder.DropTable(
                name: "AssetUnits");

            migrationBuilder.DropTable(
                name: "StockReceipts");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "AssetProducts");

            migrationBuilder.DropTable(
                name: "AssetCategories");
        }
    }
}
