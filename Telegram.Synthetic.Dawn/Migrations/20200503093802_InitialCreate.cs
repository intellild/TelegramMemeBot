using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Telegram.Synthetic.Dawn.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meme",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<int>(nullable: false),
                    content = table.Column<string>(nullable: false),
                    alias = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meme", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meme_alias",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    meme_id = table.Column<int>(nullable: false),
                    user_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meme_alias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_alias",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(nullable: false),
                    alias = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_alias", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_meme_alias",
                table: "meme",
                column: "alias");

            migrationBuilder.CreateIndex(
                name: "IX_meme_alias_meme_id",
                table: "meme_alias",
                column: "meme_id");

            migrationBuilder.CreateIndex(
                name: "IX_meme_alias_user_id",
                table: "meme_alias",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_alias_alias",
                table: "user_alias",
                column: "alias");

            migrationBuilder.CreateIndex(
                name: "IX_user_alias_user_id",
                table: "user_alias",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meme");

            migrationBuilder.DropTable(
                name: "meme_alias");

            migrationBuilder.DropTable(
                name: "user_alias");
        }
    }
}
