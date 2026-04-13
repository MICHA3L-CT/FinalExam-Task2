using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_commerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProducerEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Producer");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Producer",
                newName: "ProducerName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProducerName",
                table: "Producer",
                newName: "LastName");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Producer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
