using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEOOptimiser.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_suggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrentValue = table.Column<string>(type: "text", nullable: true),
                    SuggestedValue = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_suggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_suggestions_chat_messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "chat_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_suggestions_MessageId",
                table: "chat_suggestions",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_suggestions");
        }
    }
}
