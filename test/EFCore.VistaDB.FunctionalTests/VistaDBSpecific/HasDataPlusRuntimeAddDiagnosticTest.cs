// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Probes whether HasData (migrations IDENTITY_INSERT path) advances the IDENTITY counter so
///     subsequent runtime Add() calls receive a distinct auto-generated Id. If VistaDB leaves the
///     counter at the seed value after an IDENTITY_INSERT ON insert, the next auto-Id INSERT
///     collides with the seeded row — VistaDB might silently skip / return 0 affected rows,
///     producing the DbUpdateConcurrencyException seen in the GraphUpdates suite (which uses
///     HasData on 5+ entity types in its OnModelCreating).
/// </summary>
public class HasDataPlusRuntimeAddDiagnosticTest
{
    private readonly ITestOutputHelper _out;

    public HasDataPlusRuntimeAddDiagnosticTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public async Task HasData_seed_then_runtime_Add_with_auto_id()
    {
        using var file = new TempVistaDBFile();
        var sqlSink = new StringBuilder();

        using (var ctx = new SeedContext(file.ConnectionString, sqlSink))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        _out.WriteLine("=== After EnsureCreated (HasData seeded Parsnip Id=1) ===");
        using (var ctx = new SeedContext(file.ConnectionString, new StringBuilder()))
        {
            foreach (var p in ctx.Parsnips.ToList())
            {
                _out.WriteLine($"  Parsnip Id={p.Id} Name={p.Name}");
            }
        }

        sqlSink.Clear();

        using (var ctx = new SeedContext(file.ConnectionString, sqlSink))
        {
            ctx.Parsnips.Add(new Parsnip { Name = "Runtime-added" });

            try
            {
                var affected = await ctx.SaveChangesAsync();
                _out.WriteLine($"Runtime Add SaveChanges OK — affected={affected}");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _out.WriteLine($"*** DbUpdateConcurrencyException: {ex.Message}");
                _out.WriteLine("");
                _out.WriteLine("=== SQL at failure ===");
                _out.WriteLine(sqlSink.ToString());
                throw;
            }
            catch (DbUpdateException ex)
            {
                _out.WriteLine($"*** DbUpdateException: {ex.Message}");
                _out.WriteLine($"*** Inner: {ex.InnerException?.Message}");
                _out.WriteLine("");
                _out.WriteLine("=== SQL at failure ===");
                _out.WriteLine(sqlSink.ToString());
                throw;
            }
        }

        _out.WriteLine("=== After runtime Add: rows ===");
        using (var ctx = new SeedContext(file.ConnectionString, new StringBuilder()))
        {
            foreach (var p in ctx.Parsnips.OrderBy(p => p.Id).ToList())
            {
                _out.WriteLine($"  Parsnip Id={p.Id} Name={p.Name}");
            }
        }

        _out.WriteLine("");
        _out.WriteLine("=== Runtime Add SQL log ===");
        _out.WriteLine(sqlSink.ToString());
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

        public DbSet<Parsnip> Parsnips => Set<Parsnip>();

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseVistaDB(_cs)
                .LogTo(s => _sqlSink.AppendLine(s),
                    new[] { DbLoggerCategory.Database.Command.Name },
                    LogLevel.Information);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Parsnip>().HasData(new Parsnip { Id = 1, Name = "Seeded-via-HasData" });
    }

    private class Parsnip
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
