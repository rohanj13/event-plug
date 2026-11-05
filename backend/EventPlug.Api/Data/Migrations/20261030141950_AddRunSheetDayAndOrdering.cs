using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPlug.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRunSheetDayAndOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "day",
                table: "run_sheet_items",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "end_time",
                table: "run_sheet_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "run_sheet_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_run_sheet_items_event_id_day_sort_order",
                table: "run_sheet_items",
                columns: new[] { "event_id", "day", "sort_order" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_run_sheet_items_day_positive",
                table: "run_sheet_items",
                sql: "day >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_run_sheet_items_end_after_start",
                table: "run_sheet_items",
                sql: "end_time IS NULL OR end_time >= start_time");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_run_sheet_items_event_id_day_sort_order",
                table: "run_sheet_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_run_sheet_items_day_positive",
                table: "run_sheet_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_run_sheet_items_end_after_start",
                table: "run_sheet_items");

            migrationBuilder.DropColumn(
                name: "day",
                table: "run_sheet_items");

            migrationBuilder.DropColumn(
                name: "end_time",
                table: "run_sheet_items");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "run_sheet_items");
        }
    }
}
