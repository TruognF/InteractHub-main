using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhancePostReportWithStatusAndDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PostReports",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "Reason",
                table: "PostReports",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Detail",
                table: "PostReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReporterUserId",
                table: "PostReports",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "PostReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByAdminId",
                table: "PostReports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PostReports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PostReports_ReporterUserId",
                table: "PostReports",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostReports_ReviewedByAdminId",
                table: "PostReports",
                column: "ReviewedByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostReports_AspNetUsers_ReporterUserId",
                table: "PostReports",
                column: "ReporterUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostReports_AspNetUsers_ReviewedByAdminId",
                table: "PostReports",
                column: "ReviewedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostReports_AspNetUsers_ReporterUserId",
                table: "PostReports");

            migrationBuilder.DropForeignKey(
                name: "FK_PostReports_AspNetUsers_ReviewedByAdminId",
                table: "PostReports");

            migrationBuilder.DropIndex(
                name: "IX_PostReports_ReporterUserId",
                table: "PostReports");

            migrationBuilder.DropIndex(
                name: "IX_PostReports_ReviewedByAdminId",
                table: "PostReports");

            migrationBuilder.DropColumn(
                name: "Detail",
                table: "PostReports");

            migrationBuilder.DropColumn(
                name: "ReporterUserId",
                table: "PostReports");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "PostReports");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                table: "PostReports");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PostReports");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PostReports",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "PostReports",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
