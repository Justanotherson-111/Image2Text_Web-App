using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FixOcrJobsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                   name: "Status",
                   table: "OcrJobs",
                   nullable: false,
                   defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Progress",
                table: "OcrJobs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "OcrJobs",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "OcrJobs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "OcrJobs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "OcrJobs",
                nullable: true);

            migrationBuilder.DropColumn("Completed", "OcrJobs");
            migrationBuilder.DropColumn("ResultPath", "OcrJobs");
            migrationBuilder.DropColumn("CreatedAt", "OcrJobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
