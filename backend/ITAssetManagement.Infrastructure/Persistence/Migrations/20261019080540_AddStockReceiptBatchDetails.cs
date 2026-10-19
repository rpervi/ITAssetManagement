using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITAssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReceiptBatchDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "StockReceiptItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Good");

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "StockReceiptItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE items
                SET PurchaseDate = COALESCE(units.PurchaseDate, receipts.ReceivedDate, SYSUTCDATETIME())
                FROM dbo.StockReceiptItems AS items
                LEFT JOIN dbo.StockReceipts AS receipts ON receipts.ReceiptId = items.ReceiptId
                OUTER APPLY
                (
                    SELECT TOP (1) unit.PurchaseDate
                    FROM dbo.StockMovements AS movements
                    INNER JOIN dbo.AssetUnits AS unit ON unit.AssetUnitId = movements.AssetUnitId
                    WHERE movements.ReceiptItemId = items.ReceiptItemId
                    ORDER BY movements.MovementId
                ) AS units
                WHERE items.PurchaseDate IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PurchaseDate",
                table: "StockReceiptItems",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockReceiptItems_Condition",
                table: "StockReceiptItems",
                sql: "[Condition] IN ('New', 'Good', 'Fair', 'Poor', 'Damaged')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockReceiptItems_Condition",
                table: "StockReceiptItems");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "StockReceiptItems");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "StockReceiptItems");
        }
    }
}
