using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITAssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Employees
                    WHERE Department IS NULL OR LTRIM(RTRIM(Department)) = ''
                )
                BEGIN
                    THROW 51000, 'Employees with a missing department must be corrected before this migration.', 1;
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoiningDate",
                table: "Employees",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Designation",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "JoiningDate",
                table: "Employees");

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
