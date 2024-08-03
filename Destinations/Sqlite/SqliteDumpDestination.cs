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
using System.Diagnostics;
using System.Reflection;
using CommandLine;
using EFCore.BulkExtensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using StackExchangeDumpConverter.Destinations.EFCoreCommon;
using StackExchangeDumpConverter.Dto;

namespace StackExchangeDumpConverter.Destinations.Sqlite;

public class SqliteDumpDestination : IDumpDestination
{
    private readonly List<Badge> _badgesBuffer = new();
    private readonly List<Comment> _commentsBuffer = new();
    private readonly List<PostHistory> _postHistoriesBuffer = new();
    private readonly List<PostLink> _postLinksBuffer = new();
    private readonly List<Post> _postsBuffer = new();
    private readonly List<Tag> _tagsBuffer = new();

    private readonly List<User> _usersBuffer = new();
    private readonly List<Vote> _votesBuffer = new();
    private int _batchSize = 100000;

    private StandardDbContext? _dbContext;

    public void WriteModuleHelp()
    {
        Console.WriteLine("Sqlite Destination Options:");
        Console.WriteLine("  --sqlite-dbpath      The Sql Server to connect to.");
        Console.WriteLine("  --sqlite-replace     If true, all existing tables will be dropped and recreated.");
        Console.WriteLine(
            "                       If false, the database should already be created with a compatible schema. [Default=false]");
        Console.WriteLine(
            "  --sqlite-batch-size  The number of records to commit to the database at once. [Default=100000]");
    }

    public void Init(CommandLineParser cliArgs)
    {
        var connStrBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = cliArgs.GetValue("sqlite-dbpath"),
            DefaultTimeout = 0,
            ForeignKeys = false
        };

        _batchSize = Convert.ToInt32(cliArgs.GetValue("sqlite-batch-size") ?? "100000");

        _dbContext = new SqliteDbContext(new DbContextOptionsBuilder<StandardDbContext>()
            .LogTo(Console.WriteLine)
            .UseSqlite(connStrBuilder.ToString())
            .Options);
        if (Convert.ToBoolean(cliArgs.GetValue("sqlite-replace") ?? "false"))
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();
        }

        _dbContext.Database.OpenConnection();
        using var conn = _dbContext.Database.GetDbConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "PRAGMA synchronous=OFF; PRAGMA count_changes=OFF; PRAGMA cache_size=10000; PRAGMA journal_mode=OFF; PRAGMA temp_store=MEMORY; PRAGMA locking_mode=EXCLUSIVE; ";
        cmd.ExecuteNonQuery();
    }

    public void WriteSeedData()
    {
        Debug.Assert(_dbContext != null, nameof(_dbContext) + " != null");

        _dbContext.BulkInsert(SeedData.BadgeClasses);
        _dbContext.BulkInsert(SeedData.PostHistoryTypes);
        _dbContext.BulkInsert(SeedData.LinkTypes);
        _dbContext.BulkInsert(SeedData.PostTypes);
        _dbContext.BulkInsert(SeedData.VoteTypes);
    }

    public void Flush()
    {
        Debug.Assert(_dbContext != null, nameof(_dbContext) + " != null");

        FlushBuffer(_usersBuffer);
        FlushBuffer(_badgesBuffer);
        FlushBuffer(_postsBuffer);
        FlushBuffer(_postHistoriesBuffer);
        FlushBuffer(_commentsBuffer);
        FlushBuffer(_postLinksBuffer);
        FlushBuffer(_tagsBuffer);
        FlushBuffer(_votesBuffer);
    }

    public void StoreUser(User user)
    {
        _usersBuffer.Add(user);
        if (_usersBuffer.Count >= _batchSize) FlushBuffer(_usersBuffer);
    }

    public void StoreBadge(Badge badge)
    {
        _badgesBuffer.Add(badge);
        if (_badgesBuffer.Count >= _batchSize) FlushBuffer(_badgesBuffer);
    }

    public void StorePost(Post post)
    {
        _postsBuffer.Add(post);
        if (_postsBuffer.Count >= _batchSize) FlushBuffer(_postsBuffer);
    }

    public void StorePostHistory(PostHistory postHistory)
    {
        _postHistoriesBuffer.Add(postHistory);
        if (_postHistoriesBuffer.Count >= _batchSize) FlushBuffer(_postHistoriesBuffer);
    }

    public void StoreComment(Comment comment)
    {
        _commentsBuffer.Add(comment);
        if (_commentsBuffer.Count >= _batchSize) FlushBuffer(_commentsBuffer);
    }

    public void StorePostLink(PostLink postLink)
    {
        _postLinksBuffer.Add(postLink);
        if (_postLinksBuffer.Count >= _batchSize) FlushBuffer(_postLinksBuffer);
    }

    public void StoreTag(Tag tag)
    {
        _tagsBuffer.Add(tag);
        if (_tagsBuffer.Count >= _batchSize) FlushBuffer(_tagsBuffer);
    }

    public void StoreVote(Vote vote)
    {
        _votesBuffer.Add(vote);
        if (_votesBuffer.Count >= _batchSize) FlushBuffer(_votesBuffer);
    }

    public void Dispose()
    {
        if (_dbContext != null)
        {
            using var conn = _dbContext.Database.GetDbConnection();
            
            Console.WriteLine("Adding foreign key indices...");
            
            foreach (var entityType in _dbContext.Model.GetEntityTypes())
            {
                // Add foreign keys based on attribute.
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
                    var dbColumnName = entityType.GetProperty(foreignKey.Prop.Name!).GetColumnName();
                    
                    using var fkCmd = conn.CreateCommand();
                    fkCmd.CommandText = $"CREATE INDEX \"IX_{entityType.GetTableName()}_{dbColumnName}\" ON \"{entityType.GetTableName()}\" (\"{dbColumnName}\"); ";
                    fkCmd.ExecuteNonQuery();
                }
            }
        }

        _dbContext?.Dispose();
    }

    private void FlushBuffer<T>(List<T> buffer) where T : class
    {
        Debug.Assert(_dbContext != null, nameof(_dbContext) + " != null");

        if (buffer.Count > 0)
        {
            Console.WriteLine($"Flushing {buffer.Count} {typeof(T).Name} items...");
            _dbContext.BulkInsert(buffer);
            buffer.Clear();
        }
    }
}