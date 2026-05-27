// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Diagnostic probe (Phase 2.6 step 1 of the plan): verifies that VistaDB's SQL parser accepts
///     <c>SET IDENTITY_INSERT [Table] ON/OFF</c> and that the subsequent <c>INSERT</c> with an
///     explicit value into an IDENTITY column actually persists.
/// </summary>
/// <remarks>
///     The VistaDB.6 6.6.2 xmldoc references <c>VistaDB.Engine.SQL.SourceTable.IdentityInsert</c>
///     and the engine error "An explicit value must be provided for Identity columns when
///     IDENTITY_INSERT is ON" — these strongly suggest VistaDB supports the SqlServer pattern.
///     This test confirms by direct exercise so we can commit to the SQL implementation path for
///     <see cref="VistaDB.Update.Internal.VistaDBModificationCommandBatch" />.
/// </remarks>
public class IdentityInsertProbeTest
{
    private readonly ITestOutputHelper _out;

    public IdentityInsertProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void Set_identity_insert_accepts_explicit_id_value()
    {
        using var file = new TempVistaDBFile();

        // 1) Create the database file via the simplest path — EnsureCreated on a tiny context.
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        // 2) Probe: SET IDENTITY_INSERT [T] ON; INSERT with explicit Id; SET … OFF
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SET IDENTITY_INSERT [Probes] ON;
                INSERT INTO [Probes] ([Id], [Name]) VALUES (515, 'alpha');
                SET IDENTITY_INSERT [Probes] OFF;
                """;
            var affected = cmd.ExecuteNonQuery();
            _out.WriteLine($"INSERT with IDENTITY_INSERT — ExecuteNonQuery returned {affected}");
        }

        // 3) Verify the explicit Id persisted.
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT [Id], [Name] FROM [Probes]";
            using var reader = cmd.ExecuteReader();
            var rowCount = 0;
            while (reader.Read())
            {
                rowCount++;
                var id = reader.GetInt32(0);
                var name = reader.GetString(1);
                _out.WriteLine($"  Row {rowCount}: Id={id} Name={name}");
                Assert.Equal(515, id);
                Assert.Equal("alpha", name);
            }

            Assert.Equal(1, rowCount);
        }

        // 4) Confirm a follow-up insert WITHOUT IDENTITY_INSERT and WITHOUT supplying Id
        //    auto-generates an Id (default IDENTITY behavior is still active).
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO [Probes] ([Name]) VALUES ('beta');
                SELECT [Id] FROM [Probes] WHERE [Name] = 'beta';
                """;
            var newId = cmd.ExecuteScalar();
            _out.WriteLine($"Auto-IDENTITY follow-up — new row Id = {newId}");
            Assert.NotNull(newId);
            Assert.True((int)newId! > 515, $"Expected auto-generated Id > 515, got {newId}");
        }
    }

    private class ProbeContext : DbContext
    {
        private readonly string _cs;

        public ProbeContext(string cs)
            => _cs = cs;

        public DbSet<Probe> Probes
            => Set<Probe>();

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseVistaDB(_cs);
    }

    private class Probe
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
