using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiap.Hackatoon.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addColumnBirthOnClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Criation",
                table: "Employees",
                newName: "Creation");

            migrationBuilder.RenameColumn(
                name: "Criation",
                table: "Clients",
                newName: "Creation");

            migrationBuilder.AddColumn<DateTime>(
                name: "Birth",
                table: "Clients",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Birth",
                table: "Clients");

            migrationBuilder.RenameColumn(
                name: "Creation",
                table: "Employees",
                newName: "Criation");

            migrationBuilder.RenameColumn(
                name: "Creation",
                table: "Clients",
                newName: "Criation");
        }
    }
}
