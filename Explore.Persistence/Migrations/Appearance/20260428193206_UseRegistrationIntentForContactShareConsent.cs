using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Appearance
{
    /// <inheritdoc />
    public partial class UseRegistrationIntentForContactShareConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_event_registrations_source_eve",
                table: "event_contact_share_consents");

            migrationBuilder.RenameColumn(
                name: "source_event_registration_id",
                table: "event_contact_share_consents",
                newName: "source_event_registration_intent_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_consents_source_event_registration_id",
                table: "event_contact_share_consents",
                newName: "ix_event_contact_share_consents_source_event_registration_inte");

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_event_registration_intents_sou",
                table: "event_contact_share_consents",
                column: "source_event_registration_intent_id",
                principalTable: "event_registration_intents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_event_registration_intents_sou",
                table: "event_contact_share_consents");

            migrationBuilder.RenameColumn(
                name: "source_event_registration_intent_id",
                table: "event_contact_share_consents",
                newName: "source_event_registration_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_consents_source_event_registration_inte",
                table: "event_contact_share_consents",
                newName: "ix_event_contact_share_consents_source_event_registration_id");

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_event_registrations_source_eve",
                table: "event_contact_share_consents",
                column: "source_event_registration_id",
                principalTable: "event_registrations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
