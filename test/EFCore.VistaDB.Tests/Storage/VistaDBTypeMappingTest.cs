// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Storage;

/// <summary>
///     Unit-test surface for VistaDB type mappings (Create_and_clone_with_converter / IsRowVersion
///     parallel of <c>SqlServerTypeMappingTest</c>). Most of the SqlServer parallel asserts on
///     mapping classes that VistaDB does not currently expose (<c>SqlServerDateTimeOffsetTypeMapping</c>,
///     <c>SqlServerFloatTypeMapping</c>, etc.). Where the mapping has a VistaDB equivalent the test
///     applies as-is; where it does not, the SqlServer case is preserved verbatim in a marker block.
/// </summary>
public class VistaDBTypeMappingTest : RelationalTypeMappingTest
{
    [ConditionalTheory, InlineData(nameof(ChangeTracker.DetectChanges), false), InlineData(nameof(PropertyEntry.CurrentValue), false),
     InlineData(nameof(PropertyEntry.OriginalValue), false), InlineData(nameof(ChangeTracker.DetectChanges), true),
     InlineData(nameof(PropertyEntry.CurrentValue), true), InlineData(nameof(PropertyEntry.OriginalValue), true)]
    public void Row_version_is_marked_as_modified_only_if_it_really_changed(string mode, bool changeValue)
    {
        using var context = new OptimisticContext();
        var token = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var newToken = changeValue ? [1, 2, 3, 4, 0, 6, 7, 8] : token;

        var entity = context.Attach(
            new WithRowVersion { Id = 789, Version = token.ToArray() }).Entity;

        var propertyEntry = context.Entry(entity).Property(e => e.Version);

        Assert.Equal(token, propertyEntry.CurrentValue);
        Assert.Equal(token, propertyEntry.OriginalValue);
        Assert.False(propertyEntry.IsModified);
        Assert.Equal(EntityState.Unchanged, context.Entry(entity).State);

        switch (mode)
        {
            case nameof(ChangeTracker.DetectChanges):
                entity.Version = newToken.ToArray();
                context.ChangeTracker.DetectChanges();
                break;
            case nameof(PropertyEntry.CurrentValue):
                propertyEntry.CurrentValue = newToken.ToArray();
                break;
            case nameof(PropertyEntry.OriginalValue):
                propertyEntry.OriginalValue = newToken.ToArray();
                break;
            default:
                throw new NotImplementedException("Unexpected test mode.");
        }

        Assert.Equal(changeValue, propertyEntry.IsModified);
        Assert.Equal(changeValue ? EntityState.Modified : EntityState.Unchanged, context.Entry(entity).State);
    }

    protected override DbCommand CreateTestCommand()
        => new global::VistaDB.Provider.VistaDBCommand();

    // VistaDB: no analog — Create_and_clone_with_converter is parametrized on SqlServer-specific
    // mapping classes (SqlServerDateTimeOffsetTypeMapping, SqlServerFloatTypeMapping, etc.). The
    // VistaDB equivalents exist (VistaDBDateTimeOffsetTypeMapping, VistaDBFloatTypeMapping) but the
    // RelationalTypeMappingTest base requires the converter test to be parametrized with
    // [InlineData]; we don't override the base method here, so the relational baseline still runs.
    // Original SqlServer parametrized test preserved below for future revival.
    /*
    [ConditionalTheory, InlineData(typeof(SqlServerDateTimeOffsetTypeMapping), typeof(DateTimeOffset)),
     InlineData(typeof(SqlServerDoubleTypeMapping), typeof(double)), InlineData(typeof(SqlServerFloatTypeMapping), typeof(float)),
     InlineData(typeof(SqlServerTimeSpanTypeMapping), typeof(TimeSpan))]
    public override void Create_and_clone_with_converter(Type mappingType, Type type) { ... }
    */

    // VistaDB: no analog — VistaDB does not support sql_variant, vector, UDT, or structural JSON.
    // Original SqlServer SqlVariant/Vector/Udt/StructuralJson test cases preserved on the SqlServer
    // side for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void SqlVariant_mapping_can_be_created() { new SqlServerSqlVariantTypeMapping("sql_variant"); ... }
    [ConditionalFact]
    public void Vector_mapping_can_be_created() { new SqlServerVectorTypeMapping(); ... }
    [ConditionalFact]
    public void Udt_mapping_can_be_created() { new SqlServerUdtTypeMapping(...); ... }
    [ConditionalFact]
    public void StructuralJson_mapping_can_be_created() { new SqlServerStructuralJsonTypeMapping(); ... }
    */

    private class WithRowVersion
    {
        public int Id { get; set; }
        public byte[] Version { get; set; }
    }

    private class OptimisticContext : DbContext
    {
        public DbSet<WithRowVersion> _ { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseInternalServiceProvider(VistaDBFixture.DefaultServiceProvider)
                .UseVistaDB("Data Source=Branston.vdb6");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<WithRowVersion>().Property(e => e.Version).IsRowVersion();
    }

    protected override DbContextOptions ContextOptions { get; }
        = new DbContextOptionsBuilder()
            .UseInternalServiceProvider(VistaDBFixture.DefaultServiceProvider)
            .UseVistaDB("Data Source=Dummy.vdb6").Options;
}
