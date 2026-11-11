using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiSdlc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamAndSprintCadence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SprintWeeks",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectFileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Skills = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CapacityHours = table.Column<decimal>(type: "TEXT", precision: 7, scale: 2, nullable: false),
                    AvailabilityHours = table.Column<decimal>(type: "TEXT", precision: 7, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_ProjectFiles_ProjectFileId",
                        column: x => x.ProjectFileId,
                        principalTable: "ProjectFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_ProjectFileId",
                table: "TeamMembers",
                column: "ProjectFileId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_ProjectId",
                table: "TeamMembers",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "SprintWeeks",
                table: "Projects");
        }
    }
}
