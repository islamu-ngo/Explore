// ABOUTME: Registers the managed provisioning operation-to-tenant provenance binding pair.
// ABOUTME: Keeps customer bindings global while operation provenance remains tenant-scoped.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegisterManagedProvisioningOperationBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_external_bindings_registered_pair_scope",
                table: "external_bindings");

            migrationBuilder.AddCheckConstraint(
                name: "ck_external_bindings_registered_pair_scope",
                table: "external_bindings",
                sql: "(external_type = 'external-admin-user' AND internal_type = 'User' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-tenant-user-profile' AND internal_type = 'TenantUserProfile' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-user-actor' AND internal_type = 'Actor' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-organization' AND internal_type = 'Organization' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-group-actor' AND internal_type = 'Actor' AND scope_tenant_id IS NOT NULL) OR (external_type = 'managed-tenant-provisioning-operation' AND internal_type = 'Tenant' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-organization-actor' AND internal_type = 'Actor' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-tenant-user' AND internal_type = 'TenantUser' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-group' AND internal_type = 'Group' AND scope_tenant_id IS NOT NULL) OR (external_type = 'provider-customer' AND internal_type = 'Tenant' AND scope_tenant_id IS NULL) OR (external_type = 'external-admin-user-login' AND internal_type = 'UserExternalLogin' AND scope_tenant_id IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_external_bindings_registered_pair_scope",
                table: "external_bindings");

            migrationBuilder.AddCheckConstraint(
                name: "ck_external_bindings_registered_pair_scope",
                table: "external_bindings",
                sql: "(external_type = 'provider-customer' AND internal_type = 'Tenant' AND scope_tenant_id IS NULL) OR (external_type = 'external-admin-user' AND internal_type = 'User' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-tenant-user' AND internal_type = 'TenantUser' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-tenant-user-profile' AND internal_type = 'TenantUserProfile' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-user-actor' AND internal_type = 'Actor' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-user-login' AND internal_type = 'UserExternalLogin' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-organization' AND internal_type = 'Organization' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-organization-actor' AND internal_type = 'Actor' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-group' AND internal_type = 'Group' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-group-actor' AND internal_type = 'Actor' AND scope_tenant_id IS NOT NULL)");
        }
    }
}
