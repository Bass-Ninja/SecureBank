using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transfers_IdempotencyKey",
                table: "transfers");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "transfers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_transfers_UserId_IdempotencyKey",
                table: "transfers",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transfers_UserId_IdempotencyKey",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "transfers");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_IdempotencyKey",
                table: "transfers",
                column: "IdempotencyKey",
                unique: true);
        }
    }
}
