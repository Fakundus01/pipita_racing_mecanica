using System.IO;
using Microsoft.EntityFrameworkCore;

namespace PipitaDesktop.Data;

public static class DatabaseInitializer
{
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DatabasePathProvider.DatabaseDirectory);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={DatabasePathProvider.DatabasePath}")
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
    }
}
