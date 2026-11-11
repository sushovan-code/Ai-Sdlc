using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiSdlc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtifactVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ContentJson = table.Column<string>(type: "TEXT", nullable: false),
                    NotesJson = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Feedback = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    BasedOnVersion = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtifactVersions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArtifactVersions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ArtifactVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReviewerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReviewerRole = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DecidedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewDecisions_ArtifactVersions_ArtifactVersionId",
                        column: x => x.ArtifactVersionId,
                        principalTable: "ArtifactVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewDecisions_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactVersions_CreatedById",
                table: "ArtifactVersions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactVersions_ProjectId_Stage_Version",
                table: "ArtifactVersions",
                columns: new[] { "ProjectId", "Stage", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewDecisions_ArtifactVersionId",
                table: "ReviewDecisions",
                column: "ArtifactVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewDecisions_ReviewerId",
                table: "ReviewDecisions",
                column: "ReviewerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewDecisions");

            migrationBuilder.DropTable(
                name: "ArtifactVersions");
        }
    }
}
