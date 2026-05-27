// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     The GraphUpdates fixture uses <c>modelBuilder.Entity&lt;T&gt;().HasData(new { Id = 1, … })</c>
///     with explicit Id values. This generates <c>InsertDataOperation</c> migration operations
///     handled by <see cref="VistaDB.Migrations.VistaDBMigrationsSqlGenerator.Generate(Microsoft.EntityFrameworkCore.Migrations.Operations.InsertDataOperation, IModel?, Microsoft.EntityFrameworkCore.Migrations.MigrationCommandListBuilder, bool)" />
///     which our Fix B should wrap with <c>SET IDENTITY_INSERT</c>. Verify the row persists with
///     the explicit Id.
/// </summary>
public class HasDataIdentityInsertTest
{
    private readonly ITestOutputHelper _out;

    public HasDataIdentityInsertTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public async Task HasData_with_explicit_id_persists_via_migrations_path()
    {
        using var file = new TempVistaDBFile();
        var sqlSink = new StringBuilder();

        using (var ctx = new SeedContext(file.ConnectionString, sqlSink))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        _out.WriteLine("--- EnsureCreated SQL (showing only INSERT/SET lines) ---");
        foreach (var line in sqlSink.ToString().Split('\n'))
        {
            var t = line.TrimStart();
            if (t.StartsWith("INSERT") || t.StartsWith("SET ") || t.Contains("Executed DbCommand"))
            {
                _out.WriteLine("  " + line.TrimEnd());
            }
        }

        using (var ctx = new SeedContext(file.ConnectionString, new StringBuilder()))
        {
            var all = ctx.Beetroots.ToList();
            _out.WriteLine($"--- Read back: {all.Count} rows ---");
            foreach (var b in all)
            {
                _out.WriteLine($"  Beetroot Id={b.Id} Name={b.Name}");
            }

            Assert.Single(all);
            Assert.Equal(1, all[0].Id);
            Assert.Equal("Root One", all[0].Name);
        }
    }

    private class SeedContext : DbContext
    {
        private readonly string _cs;
        private readonly StringBuilder _sqlSink;

        public SeedContext(string cs, StringBuilder sqlSink)
        {
            _cs = cs;
            _sqlSink = sqlSink;
        }

        public DbSet<Beetroot> Beetroots => Set<Beetroot>();

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseVistaDB(_cs)
                .LogTo(s => _sqlSink.AppendLine(s),
                    new[] { DbLoggerCategory.Database.Command.Name },
                    LogLevel.Information);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Beetroot>().HasData(new Beetroot { Id = 1, Name = "Root One" });
    }

    private class Beetroot
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
