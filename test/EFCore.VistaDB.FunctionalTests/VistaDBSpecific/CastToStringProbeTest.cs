// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Probes what VistaDB accepts for CAST(intValue AS string-type) and UNICODE(SUBSTRING(...)).
///     Used to pick the right expression in VistaDBObjectToStringTranslator and
///     TranslateByteArrayElementAccess.
/// </summary>
public class CastToStringProbeTest
{
    [VistaDBInstalledFact]
    public void VistaDB_UNICODE_SUBSTRING_on_varbinary_gives_byte_value_as_int()
    {
        using var file = new TempVistaDBFile();

        // Create the .vdb6 file via DDA before opening via ADO.NET
        var dda = global::VistaDB.DDA.VistaDBEngine.Connections.OpenDDA();
        using (var db = dda.CreateDatabase(file.FilePath, false, null, 0, 0, false))
        {
            // Empty database created; DDA Dispose closes it
        }

        using var conn = new VistaDBConnection($"Data Source={file.FilePath}");
        conn.Open();

        // Create table with varbinary column
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE [T] ([Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY, [Data] varbinary(50) NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        // Insert a byte with value 101 (0x65 = 'e')
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO [T] ([Data]) VALUES (0x65)";
            cmd.ExecuteNonQuery();
        }

        // Probe: UNICODE(SUBSTRING([Data], 1, 1)) should give 101
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT UNICODE(SUBSTRING([Data], 1, 1)) FROM [T]";
            var result = cmd.ExecuteScalar();
            Assert.NotNull(result);
            var intVal = Convert.ToInt32(result);
            Assert.Equal(101, intVal);
        }

        // Probe: CAST(101 AS nvarchar) - does it work without size?
        bool castNvarcharWorks;
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CAST(101 AS nvarchar)";
            var result = cmd.ExecuteScalar()?.ToString();
            castNvarcharWorks = result == "101";
        }
        catch
        {
            castNvarcharWorks = false;
        }

        // Probe: CAST(101 AS varchar) - does it work?
        bool castVarcharWorks;
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CAST(101 AS varchar)";
            var result = cmd.ExecuteScalar()?.ToString();
            castVarcharWorks = result == "101";
        }
        catch
        {
            castVarcharWorks = false;
        }

        // Probe: CAST(101 AS nvarchar(20))
        bool castNvarcharSizedWorks;
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CAST(101 AS nvarchar(20))";
            var result = cmd.ExecuteScalar()?.ToString();
            castNvarcharSizedWorks = result == "101";
        }
        catch
        {
            castNvarcharSizedWorks = false;
        }

        // Report results - at least one must work
        Assert.True(castNvarcharWorks || castVarcharWorks || castNvarcharSizedWorks,
            $"None of the CAST approaches work! nvarchar={castNvarcharWorks}, varchar={castVarcharWorks}, nvarchar(20)={castNvarcharSizedWorks}");
    }

    [VistaDBInstalledFact]
    public void VistaDB_which_string_types_work_in_CAST()
    {
        using var file = new TempVistaDBFile();
        var dda = global::VistaDB.DDA.VistaDBEngine.Connections.OpenDDA();
        using (var db = dda.CreateDatabase(file.FilePath, false, null, 0, 0, false)) { }

        using var conn = new VistaDBConnection($"Data Source={file.FilePath}");
        conn.Open();

        var results = new Dictionary<string, (bool works, string value)>();
        foreach (var typeExpr in new[] { "nvarchar", "varchar", "ntext", "text", "nvarchar(100)", "varchar(100)", "nchar(10)", "char(10)" })
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT CAST(101 AS {typeExpr})";
                var val = cmd.ExecuteScalar()?.ToString();
                results[typeExpr] = (true, val ?? "");
            }
            catch (Exception ex)
            {
                results[typeExpr] = (false, ex.Message.Split('\n')[0]);
            }
        }

        var working = results.Where(r => r.Value.works).Select(r => $"{r.Key}='{r.Value.value}'").ToList();
        var failing = results.Where(r => !r.Value.works).Select(r => $"{r.Key}: {r.Value.value}").ToList();
        Assert.True(working.Count > 0, $"No string type works in CAST! Failing: {string.Join(", ", failing)}");
    }

    // Documents (not asserts) the byte > 127 limitation. UNICODE(SUBSTRING(varbinary)) returns U+FFFD
    // for any byte 0x80-0xFF because VistaDB treats the binary as UTF-8 and these are invalid 1-byte
    // sequences. Skip-tagged so the test suite records the limitation as a known gap rather than failure.
    [VistaDBInstalledFact(Skip = "VistaDB: UNICODE(SUBSTRING(varbinary, n, 1)) returns 65533 (U+FFFD) for bytes > 127 due to UTF-8 invalid-sequence handling. No SQL workaround; binary 0xHH literal comparison also fails. This is a VistaDB engine limitation for byte-level varbinary indexing via SQL.")]
    public void VistaDB_UNICODE_works_for_bytes_above_127()
    {
        using var file = new TempVistaDBFile();
        var dda = global::VistaDB.DDA.VistaDBEngine.Connections.OpenDDA();
        using (var db = dda.CreateDatabase(file.FilePath, false, null, 0, 0, false)) { }

        using var conn = new VistaDBConnection($"Data Source={file.FilePath}");
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE [T] ([Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY, [Data] varbinary(50) NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        // Insert bytes 201 (0xC9) and 128 (0x80)
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO [T] ([Data]) VALUES (0xC9)"; // 201
            cmd.ExecuteNonQuery();
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO [T] ([Data]) VALUES (0x80)"; // 128
            cmd.ExecuteNonQuery();
        }

        // Query: UNICODE(SUBSTRING([Data], 1, 1))
        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "SELECT [Id], UNICODE(SUBSTRING([Data], 1, 1)) FROM [T] ORDER BY [Id]";
        using var reader = cmd2.ExecuteReader();

        reader.Read();
        var firstByte = reader.GetInt32(1);
        reader.Read();
        var secondByte = reader.GetInt32(1);

        // UNICODE fails for bytes > 127 in VistaDB (UTF-8 encoding issue).
        // These will likely NOT be 201 and 128 — documenting the VistaDB limitation.
        // Instead, probe binary comparison: SUBSTRING([Data], 1, 1) = 0xC9
        bool binaryComparisonWorks;
        try
        {
            using var cmd3 = conn.CreateCommand();
            cmd3.CommandText = "SELECT COUNT(*) FROM [T] WHERE SUBSTRING([Data], 1, 1) = 0xC9";
            var count = Convert.ToInt32(cmd3.ExecuteScalar());
            binaryComparisonWorks = count == 1;
        }
        catch
        {
            binaryComparisonWorks = false;
        }

        // Document what we found
        Assert.True(binaryComparisonWorks,
            $"Binary comparison SUBSTRING([Data],1,1) = 0xHH does not work. UNICODE returned: firstByte={firstByte} (expected 201), secondByte={secondByte} (expected 128). VistaDB cannot extract bytes > 127 via UNICODE(SUBSTRING(binary)).");
    }

    [VistaDBInstalledFact]
    public void VistaDB_CAST_int_to_nvarchar_in_ORDER_BY()
    {
        using var file = new TempVistaDBFile();
        var dda = global::VistaDB.DDA.VistaDBEngine.Connections.OpenDDA();
        using (var db = dda.CreateDatabase(file.FilePath, false, null, 0, 0, false)) { }

        using var conn = new VistaDBConnection($"Data Source={file.FilePath}");
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE [T] ([Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY, [Data] varbinary(50) NOT NULL)";
            cmd.ExecuteNonQuery();
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO [T] ([Data]) VALUES (0x65)";
            cmd.ExecuteNonQuery();
        }

        // This is the query EF Core would generate for:
        // context.T.Select(c => c.Data.First().ToString()).OrderBy(n => n)
        var sql = @"SELECT CAST(UNICODE(SUBSTRING([t].[Data], 1, 1)) AS nvarchar) AS [c]
FROM [T] AS [t]
ORDER BY CAST(UNICODE(SUBSTRING([t].[Data], 1, 1)) AS nvarchar) ASC";

        // First try: CAST in both SELECT and ORDER BY
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var result = reader.GetString(0);
                Assert.Equal("101", result);
                return; // Success!
            }
        }
        catch { /* try alternatives */ }

        // Second try: CAST in SELECT, ORDER BY references the column index
        var sql3 = @"SELECT CAST(UNICODE(SUBSTRING([t].[Data], 1, 1)) AS nvarchar) AS [c]
FROM [T] AS [t]
ORDER BY 1 ASC";
        try
        {
            using var cmd3 = conn.CreateCommand();
            cmd3.CommandText = sql3;
            using var reader3 = cmd3.ExecuteReader();
            if (reader3.Read())
            {
                var result = reader3.GetString(0);
                Assert.Equal("101", result);
                return; // Success!
            }
        }
        catch { /* try alternatives */ }

        // Third try: subquery approach
        var sql4 = @"SELECT [c] FROM (SELECT CAST(UNICODE(SUBSTRING([t].[Data], 1, 1)) AS nvarchar) AS [c] FROM [T] AS [t]) AS [sub] ORDER BY [c] ASC";
        using var cmd4 = conn.CreateCommand();
        cmd4.CommandText = sql4;
        using var reader4 = cmd4.ExecuteReader();
        Assert.True(reader4.Read(), "All ORDER BY CAST approaches failed");
        var result4 = reader4.GetString(0);
        Assert.Equal("101", result4);
    }
}
