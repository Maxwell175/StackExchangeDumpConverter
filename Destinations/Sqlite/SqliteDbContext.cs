using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using StackExchangeDumpConverter.Destinations.EFCoreCommon;

namespace StackExchangeDumpConverter.Destinations.Sqlite;

public class SqliteDbContext(DbContextOptions<StandardDbContext> options) : StandardDbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
    }
    
    protected override void CustomizeModelCreating(ModelBuilder modelBuilder)
    {
        // We disagree with some EF Core conventions, so lets tweak the result.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Primary keys are not identity columns. We want to use the original keys.
            var generatedProps = entityType.GetProperties()
                .Where(prop => prop.ValueGenerated == ValueGenerated.OnAdd);
            foreach (var generatedProp in generatedProps) generatedProp.ValueGenerated = ValueGenerated.Never;

            // Properly detect foreign keys based on attribute.
            var foreignkeys = entityType.ClrType.GetProperties()
                .Select(prop => new
                {
                    Attrib = prop.GetCustomAttribute<ForeignKeyAttribute>(),
                    Prop = prop
                })
                .Where(p => p.Attrib != null)
                .ToList();
            foreach (var foreignKey in foreignkeys)
            {
                var relatedType = Assembly.GetExecutingAssembly()
                    .GetType($"{entityType.ClrType.Namespace}.{foreignKey.Attrib!.Name}")!;

                modelBuilder.Entity(entityType.ClrType)
                    .HasOne(relatedType)
                    .WithMany()
                    .HasForeignKey(foreignKey.Prop.Name)
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }
    }
}