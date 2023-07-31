// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// <ApplianceDbContextSnippet>
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace PartsInventoryConnector.Data;

public class ApplianceDbContext : DbContext
{
    public DbSet<AppliancePart> Parts => Set<AppliancePart>();

    public void EnsureDatabase()
    {
        if (Database.EnsureCreated() || !Parts.Any())
        {
            // File was just created (or is empty),
            // seed with data from CSV file
            var parts = CsvDataLoader.LoadPartsFromCsv("ApplianceParts.csv");
            Parts.AddRange(parts);
            SaveChanges();
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=parts.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // EF Core can't store lists, so add a converter for the Appliances
        // property to serialize as a JSON string on save to DB
        modelBuilder.Entity<AppliancePart>()
            .Property(ap => ap.Appliances)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default)
            );

        // Add LastUpdated and IsDeleted shadow properties
        modelBuilder.Entity<AppliancePart>()
            .Property<DateTime>("LastUpdated")
            .HasDefaultValueSql("datetime()")
            .ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<AppliancePart>()
            .Property<bool>("IsDeleted")
            .IsRequired()
            .HasDefaultValue(false);

        // Exclude any soft-deleted items (IsDeleted = 1) from
        // the default query sets
        modelBuilder.Entity<AppliancePart>()
            .HasQueryFilter(a => !EF.Property<bool>(a, "IsDeleted"));
    }

    public override int SaveChanges()
    {
        // Prevent deletes of data, instead mark the item as deleted
        // by setting IsDeleted = true.
        foreach(var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted))
        {
            if (entry.Entity.GetType() == typeof(AppliancePart))
            {
                SoftDelete(entry);
            }

        }

        return base.SaveChanges();
    }

    private void SoftDelete(EntityEntry entry)
    {
        var partNumber = new SqliteParameter("@partNumber",
            entry.OriginalValues["PartNumber"]);

        Database.ExecuteSqlRaw(
            "UPDATE Parts SET IsDeleted = 1 WHERE PartNumber = @partNumber",
            partNumber);

        entry.State = EntityState.Detached;
    }
}
// </ApplianceDbContextSnippet>
