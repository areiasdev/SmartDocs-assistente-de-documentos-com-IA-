using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDocs.Web.Migrations
{
    /// <inheritdoc />
    public partial class ConversationPerDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentId",
                table: "Conversations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "Conversations");
        }
    }
}
