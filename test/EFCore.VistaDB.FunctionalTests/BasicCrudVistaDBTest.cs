// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

public class BasicCrudVistaDBTest
{
    private class Blog
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    private class BlogContext : DbContext
    {
        private readonly string _connectionString;

        public BlogContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<Blog> Blogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseVistaDB(_connectionString);
    }

    [VistaDBInstalledFact]
    public void Insert_then_query_then_update_then_delete_round_trip_works()
    {
        using var file = new TempVistaDBFile();

        // Create
        using (var ctx = new BlogContext(file.ConnectionString))
        {
            Assert.True(ctx.Database.EnsureCreated());
            ctx.Blogs.Add(new Blog { Title = "Hello, VistaDB" });
            Assert.Equal(1, ctx.SaveChanges());
        }

        // Query
        using (var ctx = new BlogContext(file.ConnectionString))
        {
            var blog = ctx.Blogs.Single();
            Assert.True(blog.Id > 0);
            Assert.Equal("Hello, VistaDB", blog.Title);
        }

        // Update
        using (var ctx = new BlogContext(file.ConnectionString))
        {
            var blog = ctx.Blogs.Single();
            blog.Title = "Goodbye, VistaDB";
            Assert.Equal(1, ctx.SaveChanges());
        }

        using (var ctx = new BlogContext(file.ConnectionString))
        {
            Assert.Equal("Goodbye, VistaDB", ctx.Blogs.Single().Title);
        }

        // Delete row
        using (var ctx = new BlogContext(file.ConnectionString))
        {
            ctx.Blogs.RemoveRange(ctx.Blogs.ToList());
            Assert.Equal(1, ctx.SaveChanges());
        }

        using (var ctx = new BlogContext(file.ConnectionString))
        {
            Assert.Empty(ctx.Blogs);
        }

        // Drop database
        using (var ctx = new BlogContext(file.ConnectionString))
        {
            Assert.True(ctx.Database.EnsureDeleted());
        }

        Assert.False(file.Exists);
    }
}
