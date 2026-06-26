// ABOUTME: Adds acting-actor snapshot storage to AI proposed actions for auditable AI delegation.
// ABOUTME: Separates the authenticated initiator from the actor entity represented by an AI proposal.
using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(global::Explore.Persistence.ExploreDbContext))]
    [Migration("20260625143000_AddAiProposedActionActingActor")]
    public partial class AddAiProposedActionActingActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "acting_actor_id",
                table: "ai_proposed_actions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_proposed_actions_tenant_acting_actor_created_at",
                table: "ai_proposed_actions",
                columns: new[] { "tenant_id", "acting_actor_id", "created_at" },
                descending: new[] { false, false, true },
                filter: "acting_actor_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_ai_proposed_actions_actors_acting_actor_id",
                table: "ai_proposed_actions",
                column: "acting_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ai_proposed_actions_actors_acting_actor_id",
                table: "ai_proposed_actions");

            migrationBuilder.DropIndex(
                name: "ix_ai_proposed_actions_tenant_acting_actor_created_at",
                table: "ai_proposed_actions");

            migrationBuilder.DropColumn(
                name: "acting_actor_id",
                table: "ai_proposed_actions");
        }
    }
}
