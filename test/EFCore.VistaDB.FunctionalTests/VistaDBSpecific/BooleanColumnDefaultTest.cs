// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     VistaDB parses DDL more strictly than it parses expressions. The bit type mapping renders
///     <c>CAST(0 AS bit)</c> — correct for SQL Server, and accepted by VistaDB inside a query — but its
///     DDL parser rejects a CAST in a DEFAULT clause with error 285, which takes the whole
///     <c>ALTER TABLE</c> down with error 120.
///
///     That is not a corner case: it is what happens the first time anyone adds a non-nullable boolean
///     to an existing entity and runs the migration.
/// </summary>
public class BooleanColumnDefaultTest
{
    [VistaDBInstalledFact]
    public void A_bool_column_can_be_added_to_an_existing_table()
    {
        using var file = new TempVistaDBFile();

        using (var ctx = new BeforeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
            ctx.Widgets.Add(new Widget { Id = 1, Name = "existing row" });
            ctx.SaveChanges();
        }

        // The shape a migration produces: NOT NULL, with a default so the existing row has a value.
        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "ALTER TABLE [Widgets] ADD [IsActive] bit NOT NULL DEFAULT 0;";
            cmd.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM [Widgets] WHERE [IsActive] = 0;";
            Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar()));
        }
    }

    [VistaDBInstalledFact]
    public void A_cast_in_a_default_clause_is_rejected_by_the_engine()
    {
        // Pins the reason the generator special-cases booleans. If a future VistaDB build accepts
        // this, the override can go — and this test will say so by failing.
        using var file = new TempVistaDBFile();

        using (var ctx = new BeforeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE [Widgets] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);";

        Assert.ThrowsAny<Exception>(() => cmd.ExecuteNonQuery());
    }

    [VistaDBInstalledFact]
    public void Migrating_a_model_that_gains_a_bool_column_succeeds()
    {
        // The end-to-end version: EnsureCreated with the old model, then let the provider generate and
        // run the DDL for the new one.
        using var file = new TempVistaDBFile();

        using (var ctx = new BeforeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
            ctx.Widgets.Add(new Widget { Id = 1, Name = "existing row" });
            ctx.SaveChanges();
        }

        using (var ctx = new AfterContext(file.ConnectionString))
        {
            string sql = ctx.Database.GenerateCreateScript();

            // Whatever else the script contains, a boolean default must not be wrapped in a CAST.
            Assert.DoesNotContain("DEFAULT CAST", sql, StringComparison.OrdinalIgnoreCase);
        }
    }

    private class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class WidgetWithFlag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private class BeforeContext(string cs) : DbContext
    {
        public DbSet<Widget> Widgets => Set<Widget>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(cs);
        protected override void OnModelCreating(ModelBuilder b)
            => b.Entity<Widget>().Property(w => w.Id).ValueGeneratedNever();
    }

    private class AfterContext(string cs) : DbContext
    {
        public DbSet<WidgetWithFlag> Widgets => Set<WidgetWithFlag>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(cs);
        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<WidgetWithFlag>().ToTable("Widgets");
            b.Entity<WidgetWithFlag>().Property(w => w.Id).ValueGeneratedNever();
            b.Entity<WidgetWithFlag>().Property(w => w.IsActive).HasDefaultValue(false);
        }
    }
}
