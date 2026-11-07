using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPlug.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventScopeAndBudgetAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "budget_amount",
                table: "events",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Team");

            migrationBuilder.AddCheckConstraint(
                name: "ck_events_budget_amount_non_negative",
                table: "events",
                sql: "budget_amount IS NULL OR budget_amount >= 0");

            migrationBuilder.Sql("""
                UPDATE events e
                SET budget_amount = totals.allocated
                FROM (
                    SELECT event_id, SUM(allocated_amount) AS allocated
                    FROM budget_categories
                    GROUP BY event_id
                ) totals
                WHERE totals.event_id = e.id AND totals.allocated < 10000000000;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_events_budget_amount_non_negative",
                table: "events");

            migrationBuilder.DropColumn(
                name: "budget_amount",
                table: "events");

            migrationBuilder.DropColumn(
                name: "scope",
                table: "events");
        }
    }
}
