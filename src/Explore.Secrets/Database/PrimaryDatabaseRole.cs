// ABOUTME: Closed database role enum for runtime and migrator composition.
// ABOUTME: Lets the shared binder instantiate separate connection settings per role.

namespace Explore.Secrets.Database;

public enum PrimaryDatabaseRole
{
    Runtime = 1,
    Migrator = 2,
}
