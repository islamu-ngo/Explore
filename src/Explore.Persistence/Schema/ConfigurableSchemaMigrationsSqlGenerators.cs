// ABOUTME: Rebinds canonical generated migration schemas to the validated runtime schema.
// ABOUTME: Keeps PostgreSQL and SQL Server migrations generated once while supporting operator namespaces.

using System.Collections;
using System.Reflection;
using Explore.Persistence.Database;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microting.EntityFrameworkCore.MySql.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
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

internal sealed class ConfigurableSqliteMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    IRelationalAnnotationProvider migrationsAnnotations)
    : SqliteMigrationsSqlGenerator(dependencies, migrationsAnnotations)
{
    public override IReadOnlyList<MigrationCommand> Generate(
        IReadOnlyList<MigrationOperation> operations,
        Microsoft.EntityFrameworkCore.Metadata.IModel? model = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
        => ConfigurableSchemaMigrationOperations.AppendPromotionCodeBackfill(
            base.Generate(operations, model, options),
            operations,
            Dependencies);
}

internal sealed class ConfigurableMySqlMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    ICommandBatchPreparer commandBatchPreparer,
    IMySqlOptions options)
    : MySqlMigrationsSqlGenerator(dependencies, commandBatchPreparer, options)
{
    public override IReadOnlyList<MigrationCommand> Generate(
        IReadOnlyList<MigrationOperation> operations,
        Microsoft.EntityFrameworkCore.Metadata.IModel? model = null,
        MigrationsSqlGenerationOptions sqlOptions = MigrationsSqlGenerationOptions.Default)
        => ConfigurableSchemaMigrationOperations.AppendPromotionCodeBackfill(
            base.Generate(operations, model, sqlOptions),
            operations,
            Dependencies);
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
        commands = AppendPromotionCodeBackfill(commands, [], dependencies);
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

    public static IReadOnlyList<MigrationCommand> AppendPromotionCodeBackfill(
        IReadOnlyList<MigrationCommand> commands,
        IReadOnlyList<MigrationOperation> operations,
        MigrationsSqlGeneratorDependencies dependencies)
    {
        if (CommandTextContainsPromotionCodeBackfill(commands) ||
            (!AddsPromotionCodeSnapshotColumns(operations) && !CommandTextAddsPromotionCodeSnapshotColumns(commands)))
        {
            return commands;
        }

        if (commands.Count == 0)
        {
            return commands;
        }

        var backfill = BuildPromotionCodeBackfillSql(dependencies.CurrentContext.Context);
        var relationalCommand = dependencies.CommandBuilderFactory.Create()
            .Append(backfill)
            .Build();
        return commands
            .Concat([
                new MigrationCommand(
                    relationalCommand,
                    dependencies.CurrentContext.Context,
                    commands[^1].CommandLogger,
                    transactionSuppressed: false)
            ])
            .ToArray();
    }

    private static string GetConfiguredSchema(Microsoft.EntityFrameworkCore.DbContext context) =>
        context.GetService<IDbContextOptions>()
            .FindExtension<RelationalNamespaceOptionsExtension>()?.TargetSchema
        ?? RelationalModelNamespace.DefaultSchema;

    private static bool AddsPromotionCodeSnapshotColumns(IReadOnlyList<MigrationOperation> operations) =>
        operations.OfType<AddColumnOperation>().Any(operation =>
            StringComparer.Ordinal.Equals(operation.Table, "registration_orders") &&
            StringComparer.Ordinal.Equals(operation.Name, "pre_discount_organizer_directed_total_minor_snapshot")) &&
        operations.OfType<AddColumnOperation>().Any(operation =>
            StringComparer.Ordinal.Equals(operation.Table, "registration_order_lines") &&
            StringComparer.Ordinal.Equals(operation.Name, "pre_discount_line_subtotal_minor_snapshot"));

    private static bool CommandTextAddsPromotionCodeSnapshotColumns(IReadOnlyList<MigrationCommand> commands)
    {
        var text = string.Join('\n', commands.Select(command => command.CommandText));
        return text.Contains("pre_discount_organizer_directed_total_minor_snapshot", StringComparison.Ordinal) &&
               text.Contains("pre_discount_line_subtotal_minor_snapshot", StringComparison.Ordinal) &&
               !text.Contains("line_subtotal_snapshot\"", StringComparison.Ordinal) &&
               !text.Contains("line_subtotal_snapshot]", StringComparison.Ordinal) &&
               !text.Contains("line_subtotal_snapshot`", StringComparison.Ordinal);
    }

    private static bool CommandTextContainsPromotionCodeBackfill(IReadOnlyList<MigrationCommand> commands)
    {
        var text = string.Join('\n', commands.Select(command => command.CommandText));
        return text.Contains("organizer_directed_total_minor_snapshot", StringComparison.Ordinal) &&
               text.Contains("line_subtotal_snapshot", StringComparison.Ordinal);
    }

    private static string BuildPromotionCodeBackfillSql(Microsoft.EntityFrameworkCore.DbContext context)
    {
        var provider = context.Database.ProviderName ?? string.Empty;
        var prefix = UsesPrefixedTables(provider) ? RelationalModelNamespace.Prefix : string.Empty;
        var schema = UsesSchemas(provider) ? GetConfiguredSchema(context) : null;
        var quote = QuoteStyle(provider);
        var orders = Table(schema, prefix + "registration_orders", quote);
        var lines = Table(schema, prefix + "registration_order_lines", quote);

        return $"""
               UPDATE {orders}
               SET {Identifier("pre_discount_organizer_directed_total_minor_snapshot", quote)} = {Identifier("organizer_directed_total_minor_snapshot", quote)},
                   {Identifier("post_discount_organizer_directed_total_minor_snapshot", quote)} = {Identifier("organizer_directed_total_minor_snapshot", quote)};
               UPDATE {lines}
               SET {Identifier("pre_discount_line_subtotal_minor_snapshot", quote)} = {Identifier("line_subtotal_snapshot", quote)},
                   {Identifier("post_discount_line_subtotal_minor_snapshot", quote)} = {Identifier("line_subtotal_snapshot", quote)};
               """;
    }

    private static bool UsesSchemas(string provider) =>
        provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ||
        provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);

    private static bool UsesPrefixedTables(string provider) => !UsesSchemas(provider);

    private static string QuoteStyle(string provider) =>
        provider.Contains("MySql", StringComparison.OrdinalIgnoreCase) ? "`" :
        provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ? "[]" :
        "\"";

    private static string Table(string? schema, string name, string quote) => schema is null
        ? Identifier(name, quote)
        : $"{Identifier(schema, quote)}.{Identifier(name, quote)}";

    private static string Identifier(string value, string quote) => quote == "[]"
        ? $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]"
        : $"{quote}{value.Replace(quote, quote + quote, StringComparison.Ordinal)}{quote}";

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
