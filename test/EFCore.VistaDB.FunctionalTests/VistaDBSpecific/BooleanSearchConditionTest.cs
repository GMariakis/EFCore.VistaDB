// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     VistaDB has no boolean type: a <c>bit</c> column is a value, and the engine rejects it where a
///     condition is expected with error 666, "a column is referenced in a context where a boolean
///     condition is expected". Conversely a predicate cannot be projected out as a value.
///     <see cref="VistaDB.Query.Internal.VistaDBSearchConditionConverter" /> reconciles the two, and
///     these tests are what hold it honest — each one throws 666 (or its mirror image) without it.
/// </summary>
public class BooleanSearchConditionTest
{
    [VistaDBInstalledFact]
    public void Bare_boolean_column_in_where()
    {
        using var ctx = Seeded();

        // WHERE [w].[IsActive] => WHERE [w].[IsActive] = CAST(1 AS bit)
        Assert.Equal([1, 3], ctx.Widgets.Where(w => w.IsActive).Select(w => w.Id).OrderBy(id => id).ToList());
    }

    [VistaDBInstalledFact]
    public void Negated_boolean_column_in_where()
    {
        using var ctx = Seeded();

        Assert.Equal([2, 4], ctx.Widgets.Where(w => !w.IsActive).Select(w => w.Id).OrderBy(id => id).ToList());
    }

    [VistaDBInstalledFact]
    public void Boolean_column_compared_to_constant()
    {
        using var ctx = Seeded();

        // EF collapses "== true" back to the bare column before the converter runs, which is exactly
        // why the conversion cannot be expressed in LINQ by the caller.
        Assert.Equal([1, 3], ctx.Widgets.Where(w => w.IsActive == true).Select(w => w.Id).OrderBy(id => id).ToList());
        Assert.Equal([2, 4], ctx.Widgets.Where(w => w.IsActive == false).Select(w => w.Id).OrderBy(id => id).ToList());
    }

    [VistaDBInstalledFact]
    public void Boolean_columns_combined_with_and_or()
    {
        using var ctx = Seeded();

        Assert.Equal([1], ctx.Widgets.Where(w => w.IsActive && w.IsArchived).Select(w => w.Id).ToList());
        Assert.Equal(
            [1, 2, 3], ctx.Widgets.Where(w => w.IsActive || w.IsArchived).Select(w => w.Id).OrderBy(id => id).ToList());
        Assert.Equal([3], ctx.Widgets.Where(w => w.IsActive && !w.IsArchived).Select(w => w.Id).ToList());
    }

    [VistaDBInstalledFact]
    public void Boolean_mixed_with_a_comparison_and_a_contains()
    {
        using var ctx = Seeded();

        // The shape that motivated the fix: an IN list plus a negated flag.
        int[] ids = [1, 2, 3];
        Assert.Equal([2], ctx.Widgets.Where(w => ids.Contains(w.Id) && !w.IsActive).Select(w => w.Id).ToList());

        Assert.Equal([3], ctx.Widgets.Where(w => w.IsActive && w.Quantity > 5).Select(w => w.Id).ToList());
    }

    [VistaDBInstalledFact]
    public void Nullable_boolean_column()
    {
        using var ctx = Seeded();

        Assert.Equal([1], ctx.Widgets.Where(w => w.IsApproved == true).Select(w => w.Id).ToList());
        Assert.Equal([2], ctx.Widgets.Where(w => w.IsApproved == false).Select(w => w.Id).ToList());
        Assert.Equal([3, 4], ctx.Widgets.Where(w => w.IsApproved == null).Select(w => w.Id).OrderBy(id => id).ToList());
    }

    [VistaDBInstalledFact]
    public void Boolean_column_projected_as_a_value()
    {
        using var ctx = Seeded();

        // The reverse direction: a bit column is already a value, so it must NOT be wrapped.
        Assert.Equal([true, false, true, false], ctx.Widgets.OrderBy(w => w.Id).Select(w => w.IsActive).ToList());
    }

    [VistaDBInstalledFact]
    public void Predicate_projected_as_a_value()
    {
        using var ctx = Seeded();

        // SELECT [w].[Quantity] > 5 => SELECT CASE WHEN [w].[Quantity] > 5 THEN 1 ELSE 0 END
        Assert.Equal([false, false, true, true], ctx.Widgets.OrderBy(w => w.Id).Select(w => w.Quantity > 5).ToList());

        // Negation of a column, projected.
        Assert.Equal([false, true, false, true], ctx.Widgets.OrderBy(w => w.Id).Select(w => !w.IsActive).ToList());
    }

    [VistaDBInstalledFact]
    public void Predicate_used_as_a_case_test()
    {
        using var ctx = Seeded();

        Assert.Equal(
            ["on", "off", "on", "off"],
            ctx.Widgets.OrderBy(w => w.Id).Select(w => w.IsActive ? "on" : "off").ToList());
    }

    [VistaDBInstalledFact]
    public void Boolean_column_in_order_by()
    {
        using var ctx = Seeded();

        // ORDER BY wants a value, not a condition.
        Assert.Equal(
            [2, 4, 1, 3], ctx.Widgets.OrderBy(w => w.IsActive).ThenBy(w => w.Id).Select(w => w.Id).ToList());
    }

    [VistaDBInstalledFact]
    public void Boolean_column_in_any_all_and_count()
    {
        using var ctx = Seeded();

        Assert.True(ctx.Widgets.Any(w => w.IsActive));
        Assert.False(ctx.Widgets.All(w => w.IsActive));
        Assert.Equal(2, ctx.Widgets.Count(w => w.IsActive));
    }

    [VistaDBInstalledFact]
    public void Boolean_column_in_a_join_predicate_and_a_subquery()
    {
        using var ctx = Seeded();

        // A navigation filtered on a bool puts the condition in a subquery/join.
        Assert.Equal(
            [1], ctx.Widgets.Where(w => w.Parts.Any(p => p.IsFaulty)).Select(w => w.Id).ToList());

        // Widget 1 is the only one that is archived AND has a faulty part, so it is the only exclusion.
        Assert.Equal(
            [2, 3, 4],
            ctx.Widgets.Where(w => !w.Parts.Any(p => p.IsFaulty && w.IsArchived)).Select(w => w.Id).OrderBy(id => id).ToList());
    }

    [VistaDBInstalledFact]
    public void Boolean_column_in_group_by_and_having()
    {
        using var ctx = Seeded();

        var byFlag = ctx.Widgets
            .GroupBy(w => w.IsActive)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderBy(x => x.Key)
            .ToList();

        Assert.Equal(2, byFlag.Count);
        Assert.False(byFlag[0].Key);
        Assert.Equal(2, byFlag[0].Count);
        Assert.True(byFlag[1].Key);
        Assert.Equal(2, byFlag[1].Count);
    }

    private static WidgetContext Seeded()
    {
        var file = new TempVistaDBFile();
        var ctx = new WidgetContext(file);
        ctx.Database.EnsureCreated();

        ctx.Widgets.AddRange(
            new Widget { Id = 1, IsActive = true, IsArchived = true, IsApproved = true, Quantity = 1 },
            new Widget { Id = 2, IsActive = false, IsArchived = true, IsApproved = false, Quantity = 2 },
            new Widget { Id = 3, IsActive = true, IsArchived = false, IsApproved = null, Quantity = 10 },
            new Widget { Id = 4, IsActive = false, IsArchived = false, IsApproved = null, Quantity = 20 });

        ctx.Parts.AddRange(
            new Part { Id = 1, WidgetId = 1, IsFaulty = true },
            new Part { Id = 2, WidgetId = 2, IsFaulty = false });

        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();
        return ctx;
    }

    private class Widget
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public bool IsArchived { get; set; }
        public bool? IsApproved { get; set; }
        public int Quantity { get; set; }
        public ICollection<Part> Parts { get; set; } = new List<Part>();
    }

    private class Part
    {
        public int Id { get; set; }
        public int WidgetId { get; set; }
        public bool IsFaulty { get; set; }
    }

    private class WidgetContext(TempVistaDBFile file) : DbContext
    {
        public DbSet<Widget> Widgets => Set<Widget>();
        public DbSet<Part> Parts => Set<Part>();

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseVistaDB(file.ConnectionString);

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Explicit keys so the seed can control ids without identity insert.
            b.Entity<Widget>().Property(w => w.Id).ValueGeneratedNever();
            b.Entity<Part>().Property(p => p.Id).ValueGeneratedNever();
            b.Entity<Part>().HasOne<Widget>().WithMany(w => w.Parts).HasForeignKey(p => p.WidgetId);
        }

        public override void Dispose()
        {
            base.Dispose();
            file.Dispose();
        }
    }
}
