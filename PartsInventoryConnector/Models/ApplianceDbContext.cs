using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PartsInventoryConnector.Models
{
    public class ApplianceDbContext : DbContext
    {
        public DbSet<AppliancePart> Parts { get; set; }

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
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<List<string>>(v)
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
}