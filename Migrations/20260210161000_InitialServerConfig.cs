using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VictorNovember.Migrations
{
    /// <inheritdoc />
    public partial class InitialServerConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WelcomeChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: true),
                    WelcomeBannerUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Servers");
        }
    }
}
