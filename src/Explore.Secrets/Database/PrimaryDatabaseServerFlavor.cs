// ABOUTME: Bounded server flavor for MariaDB and MySQL composition.
// ABOUTME: Separates engine flavor from version so validation can fail closed.

namespace Explore.Secrets.Database;

public enum PrimaryDatabaseServerFlavor
{
    MariaDb = 1,
    MySql = 2,
}
