using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VictorNovember.Migrations
{
    /// <inheritdoc />
    public partial class AddHoneypot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HoneypotConfigs",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    ChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    ModLogChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    WarningMessageId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    CounterMessageId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    HitCount = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    ConfiguredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfiguredByUserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoneypotConfigs", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "HoneypotHits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuildId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MessageContent = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AttachmentUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasBanned = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoneypotHits", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoneypotConfigs");

            migrationBuilder.DropTable(
                name: "HoneypotHits");
        }
    }
}
