/*
 * StackExchangeDumpConverter
 * Copyright (C) 2024 Maxwell Dreytser
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StackExchangeDumpConverter.Dto;

namespace StackExchangeDumpConverter.Destinations.EFCoreCommon;

public class StandardDbContext(DbContextOptions<StandardDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<BadgeClass> BadgeClasses { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<PostType> PostTypes { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<PostHistoryType> PostHistoryTypes { get; set; }
    public DbSet<PostHistory> PostHistories { get; set; }
    public DbSet<VoteType> VoteTypes { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<LinkType> LinkTypes { get; set; }
    public DbSet<PostLink> PostLinks { get; set; }
    public DbSet<Comment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        CustomizeModelCreating(modelBuilder);
    }

    protected virtual void CustomizeModelCreating(ModelBuilder modelBuilder)
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

                // Circular references get a bit whacky so add them later.
                if (relatedType == entityType.ClrType) continue;

                modelBuilder.Entity(entityType.ClrType)
                    .HasOne(relatedType)
                    .WithMany()
                    .HasForeignKey(foreignKey.Prop.Name)
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }
    }
}