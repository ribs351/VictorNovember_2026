using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VictorNovember.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchUsages_MonthKey",
                table: "SearchUsages",
                column: "MonthKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchUsages");
        }
    }
}
