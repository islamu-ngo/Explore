// ABOUTME: Canonical keys for sensitive infrastructure credentials that require stricter handling boundaries.
// ABOUTME: Separates secret-bearing settings from functional governance keys to reduce accidental exposure risk.

namespace Explore.Domain.Constants;

public static class InfrastructureSecretSettingKeys
{
    public static class Email
    {
        public const string SmtpUsername = "email.smtp_username";
        public const string SmtpPassword = "email.smtp_password";
    }

    public static class Storage
    {
        public const string AccessKeyId = "s3.access_key_id";
        public const string SecretAccessKey = "s3.secret_access_key";
    }

    public static class Cerbos
    {
        public const string CustomAdminUsername = "cerbos.custom_admin_username";
        public const string CustomAdminPassword = "cerbos.custom_admin_password";
    }

    public static class Authentication
    {
        public const string KeycloakClientSecret = "auth.keycloak_client_secret";
        public const string GoogleClientSecret = "auth.google_client_secret";
    }
}
