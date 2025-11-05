using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoIpApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProcessedFromBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Processed",
                table: "Batches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Processed",
                table: "Batches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
