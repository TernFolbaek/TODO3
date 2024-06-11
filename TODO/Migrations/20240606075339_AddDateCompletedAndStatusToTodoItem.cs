using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TODO.Migrations
{
    /// <inheritdoc />
    public partial class AddDateCompletedAndStatusToTodoItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateCompleted",
                table: "TodoItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TodoItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCompleted",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TodoItems");
        }
    }
}
