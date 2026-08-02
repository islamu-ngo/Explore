// ABOUTME: Rebinds canonical generated migration schemas to the validated runtime schema.
// ABOUTME: Keeps PostgreSQL and SQL Server migrations generated once while supporting operator namespaces.

using System.Collections;
using System.Reflection;
using Explore.Persistence.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Update;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace Explore.Persistence.Schema;

internal sealed class ConfigurableNpgsqlMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    INpgsqlSingletonOptions npgsqlOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlOptions)
{
    public override IReadOnlyList<MigrationCommand> Generate(
        IReadOnlyList<MigrationOperation> operations,
        Microsoft.EntityFrameworkCore.Metadata.IModel? model = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
        ConfigurableSchemaMigrationOperations.Rewrite(operations, Dependencies.CurrentContext.Context);
        return ConfigurableSchemaMigrationOperations.RewriteCommands(
            base.Generate(operations, model, options),
            Dependencies);
    }
}

internal sealed class ConfigurableSqlServerMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    ICommandBatchPreparer commandBatchPreparer)
    : SqlServerMigrationsSqlGenerator(dependencies, commandBatchPreparer)
{
    public override IReadOnlyList<MigrationCommand> Generate(
        IReadOnlyList<MigrationOperation> operations,
        Microsoft.EntityFrameworkCore.Metadata.IModel? model = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
        ConfigurableSchemaMigrationOperations.Rewrite(operations, Dependencies.CurrentContext.Context);
        return ConfigurableSchemaMigrationOperations.RewriteCommands(
            base.Generate(operations, model, options),
            Dependencies);
    }
}

internal static class ConfigurableSchemaMigrationOperations
{
    public static void Rewrite(IReadOnlyList<MigrationOperation> operations, Microsoft.EntityFrameworkCore.DbContext context)
    {
        var configuredSchema = GetConfiguredSchema(context);
        if (StringComparer.Ordinal.Equals(configuredSchema, RelationalModelNamespace.DefaultSchema))
        {
            return;
        }

        foreach (var operation in operations)
        {
            RewriteOperation(operation, configuredSchema);
        }
    }

    public static IReadOnlyList<MigrationCommand> RewriteCommands(
        IReadOnlyList<MigrationCommand> commands,
        MigrationsSqlGeneratorDependencies dependencies)
    {
        var configuredSchema = GetConfiguredSchema(dependencies.CurrentContext.Context);
        if (StringComparer.Ordinal.Equals(configuredSchema, RelationalModelNamespace.DefaultSchema))
        {
            return commands;
        }

        return commands.Select(command =>
        {
            var commandText = command.CommandText.Replace(
                RelationalModelNamespace.DefaultSchema,
                configuredSchema,
                StringComparison.Ordinal);
            var relationalCommand = dependencies.CommandBuilderFactory.Create()
                .Append(commandText)
                .Build();
            return new MigrationCommand(
                relationalCommand,
                dependencies.CurrentContext.Context,
                command.CommandLogger,
                command.TransactionSuppressed);
        }).ToArray();
    }

    private static string GetConfiguredSchema(Microsoft.EntityFrameworkCore.DbContext context) =>
        context.GetService<IDbContextOptions>()
            .FindExtension<RelationalNamespaceOptionsExtension>()?.TargetSchema
        ?? RelationalModelNamespace.DefaultSchema;

    private static void RewriteOperation(MigrationOperation operation, string configuredSchema)
    {
        if (operation is EnsureSchemaOperation ensureSchema &&
            StringComparer.Ordinal.Equals(ensureSchema.Name, RelationalModelNamespace.DefaultSchema))
        {
            ensureSchema.Name = configuredSchema;
        }
        else if (operation is DropSchemaOperation dropSchema &&
                 StringComparer.Ordinal.Equals(dropSchema.Name, RelationalModelNamespace.DefaultSchema))
        {
            dropSchema.Name = configuredSchema;
        }

        foreach (var property in operation.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            if (property.CanRead && property.CanWrite && property.PropertyType == typeof(string) &&
                property.GetValue(operation) is string schema &&
                StringComparer.Ordinal.Equals(schema, RelationalModelNamespace.DefaultSchema))
            {
                property.SetValue(operation, configuredSchema);
            }

            if (!property.CanRead || property.PropertyType == typeof(string) ||
                property.GetValue(operation) is not IEnumerable children)
            {
                continue;
            }

            foreach (var child in children.OfType<MigrationOperation>())
            {
                RewriteOperation(child, configuredSchema);
            }
        }
    }
}
