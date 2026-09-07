// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Rows-affected reporting, in both directions.
///
///     The generator used to end every write with <c>SELECT @@ROWCOUNT</c>, which is not a supported
///     VistaDB expression — the engine documents only @@IDENTITY, @@VERSION and @@TRANCOUNT, and the
///     6.6.0 notes record a fix for it miscounting in a multi-statement batch. When it under-reported,
///     EF Core raised a concurrency error for a row that had in fact been written.
///
///     These tests pin both halves: a write that succeeds must not throw, and a write that really does
///     hit nothing must still be caught.
/// </summary>
public class RowsAffectedTest
{
    [VistaDBInstalledFact]
    public void Inserting_a_row_does_not_report_a_phantom_conflict()
    {
        using var file = new TempVistaDBFile();

        using var ctx = new WidgetContext(file.ConnectionString);
        ctx.Database.EnsureCreated();

        ctx.Widgets.Add(new Widget { Id = Guid.NewGuid(), Name = "first" });

        // The failure this guards: "expected to affect 1 row(s), but actually affected 0 row(s)".
        ctx.SaveChanges();

        Assert.Equal(1, ctx.Widgets.Count());
    }

    [VistaDBInstalledFact]
    public void Inserting_several_rows_in_one_batch_works()
    {
        // A multi-statement batch is where @@ROWCOUNT was documented to go wrong.
        using var file = new TempVistaDBFile();

        using var ctx = new WidgetContext(file.ConnectionString);
        ctx.Database.EnsureCreated();

        for (var i = 0; i < 5; i++)
        {
            ctx.Widgets.Add(new Widget { Id = Guid.NewGuid(), Name = $"row {i}" });
        }

        ctx.SaveChanges();

        Assert.Equal(5, ctx.Widgets.Count());
    }

    [VistaDBInstalledFact]
    public void Updating_a_row_does_not_report_a_phantom_conflict()
    {
        using var file = new TempVistaDBFile();
        var id = Guid.NewGuid();

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
            ctx.Widgets.Add(new Widget { Id = id, Name = "before" });
            ctx.SaveChanges();
        }

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            Widget widget = ctx.Widgets.Single(w => w.Id == id);
            widget.Name = "after";
            ctx.SaveChanges();
        }

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            Assert.Equal("after", ctx.Widgets.Single(w => w.Id == id).Name);
        }
    }

    [VistaDBInstalledFact]
    public void Deleting_a_row_does_not_report_a_phantom_conflict()
    {
        using var file = new TempVistaDBFile();
        var id = Guid.NewGuid();

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
            ctx.Widgets.Add(new Widget { Id = id, Name = "doomed" });
            ctx.SaveChanges();
        }

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            ctx.Widgets.Remove(ctx.Widgets.Single(w => w.Id == id));
            ctx.SaveChanges();
            Assert.Empty(ctx.Widgets);
        }
    }

    [VistaDBInstalledFact]
    public void Mixed_inserts_updates_and_deletes_in_one_save_all_land()
    {
        using var file = new TempVistaDBFile();
        var keep = Guid.NewGuid();
        var drop = Guid.NewGuid();

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
            ctx.Widgets.AddRange(
                new Widget { Id = keep, Name = "before" },
                new Widget { Id = drop, Name = "doomed" });
            ctx.SaveChanges();
        }

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            ctx.Widgets.Single(w => w.Id == keep).Name = "after";
            ctx.Widgets.Remove(ctx.Widgets.Single(w => w.Id == drop));
            ctx.Widgets.Add(new Widget { Id = Guid.NewGuid(), Name = "new" });

            ctx.SaveChanges();
        }

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            Assert.Equal(2, ctx.Widgets.Count());
            Assert.Equal("after", ctx.Widgets.Single(w => w.Id == keep).Name);
        }
    }

    [VistaDBInstalledFact]
    public void A_real_conflict_is_still_detected()
    {
        // The other half. Dropping @@ROWCOUNT must not mean silently accepting a write that hit
        // nothing — ExecuteNonQuery has to be reporting a genuine count, not a constant.
        using var file = new TempVistaDBFile();
        var id = Guid.NewGuid();

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
            ctx.Widgets.Add(new Widget { Id = id, Name = "here" });
            ctx.SaveChanges();
        }

        using var first = new WidgetContext(file.ConnectionString);
        Widget tracked = first.Widgets.Single(w => w.Id == id);

        // Deleted out from under the first context, so its update matches no rows.
        using (var second = new WidgetContext(file.ConnectionString))
        {
            second.Widgets.Remove(second.Widgets.Single(w => w.Id == id));
            second.SaveChanges();
        }

        tracked.Name = "changed";

        // DbUpdateException, not DbUpdateConcurrencyException: the provider deliberately throws the
        // base type — see ThrowAggregateUpdateConcurrencyException for why. Worth revisiting now the
        // count is trustworthy, since a 0 here really does mean a conflict.
        Assert.Throws<DbUpdateException>(() => first.SaveChanges());
    }

    [VistaDBInstalledFact]
    public void Deleting_a_row_that_is_already_gone_is_still_a_conflict()
    {
        using var file = new TempVistaDBFile();
        var id = Guid.NewGuid();

        using (var ctx = new WidgetContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
            ctx.Widgets.Add(new Widget { Id = id, Name = "here" });
            ctx.SaveChanges();
        }

        using var first = new WidgetContext(file.ConnectionString);
        Widget tracked = first.Widgets.Single(w => w.Id == id);

        using (var second = new WidgetContext(file.ConnectionString))
        {
            second.Widgets.Remove(second.Widgets.Single(w => w.Id == id));
            second.SaveChanges();
        }

        first.Widgets.Remove(tracked);

        Assert.Throws<DbUpdateException>(() => first.SaveChanges());
    }

    private class Widget
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class WidgetContext(string cs) : DbContext
    {
        public DbSet<Widget> Widgets => Set<Widget>();

        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(cs);

        protected override void OnModelCreating(ModelBuilder b)
            // Client-generated key, so there is nothing to read back and the write takes the bare
            // statement path — which is the one that was broken.
            => b.Entity<Widget>().Property(w => w.Id).ValueGeneratedNever();
    }
}
