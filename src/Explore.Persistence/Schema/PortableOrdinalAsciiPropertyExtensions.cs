// ABOUTME: Marks ASCII-derived keys for provider-native binary collation without ambient database semantics.
// ABOUTME: Keeps the marker targeted so unrelated PostgreSQL collation normalization remains unchanged.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Schema;

internal static class PortableOrdinalAsciiPropertyExtensions
{
    internal const string AnnotationName = "Explore:PortableOrdinalAscii";

    internal static PropertyBuilder<TProperty> UsePortableOrdinalAscii<TProperty>(
        this PropertyBuilder<TProperty> property)
        where TProperty : class? =>
        property.UseCollation("C").HasAnnotation(AnnotationName, true);
}
