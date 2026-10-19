using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITAssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetAssignmentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "AssetAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssetAssignmentBatchAllocations",
                columns: table => new
                {
                    AllocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentItemId = table.Column<int>(type: "int", nullable: false),
                    ReceiptItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignmentBatchAllocations", x => x.AllocationId);
                    table.CheckConstraint("CK_AssetAssignmentBatchAllocations_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_AssetAssignmentBatchAllocations_AssetAssignmentItems_AssignmentItemId",
                        column: x => x.AssignmentItemId,
                        principalTable: "AssetAssignmentItems",
                        principalColumn: "AssignmentItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetAssignmentBatchAllocations_StockReceiptItems_ReceiptItemId",
                        column: x => x.ReceiptItemId,
                        principalTable: "StockReceiptItems",
                        principalColumn: "ReceiptItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_IdempotencyKey",
                table: "AssetAssignments",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentBatchAllocations_AssignmentItemId",
                table: "AssetAssignmentBatchAllocations",
                column: "AssignmentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentBatchAllocations_ReceiptItemId",
                table: "AssetAssignmentBatchAllocations",
                column: "ReceiptItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetAssignmentBatchAllocations");

            migrationBuilder.DropIndex(
                name: "IX_AssetAssignments_IdempotencyKey",
                table: "AssetAssignments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "AssetAssignments");
        }
    }
}
