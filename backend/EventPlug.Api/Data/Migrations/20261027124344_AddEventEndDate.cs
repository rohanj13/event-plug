using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPlug.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "end_date",
                table: "events",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_events_end_date_after_event_date",
                table: "events",
                sql: "end_date IS NULL OR end_date >= event_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_events_end_date_after_event_date",
                table: "events");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "events");
        }
    }
}
