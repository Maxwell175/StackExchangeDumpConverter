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

using System.Diagnostics;
using CommandLine;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchangeDumpConverter.Destinations.EFCoreCommon;
using StackExchangeDumpConverter.Dto;

namespace StackExchangeDumpConverter.Destinations.Postgres;

public class PostgresDumpDestination : IDumpDestination
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
        Console.WriteLine("Postgres Destination Options:");
        Console.WriteLine("  --postgres-host        The hostname of the PostgreSQL server. [Default=localhost]");
        Console.WriteLine("  --postgres-port        The port of the PostgreSQL server to connect to. [Default=5432]");
        Console.WriteLine("  --postgres-user        The username to authenticate with.");
        Console.WriteLine("  --postgres-pass        The password to authenticate with.");
        Console.WriteLine("  --postgres-db          The database that the data will be loaded into.");
        Console.WriteLine("  --postgres-replace     If true, all existing tables will be dropped and recreated.");
        Console.WriteLine(
            "                         If false, the database should already be created with a compatible schema. [Default=false]");
        Console.WriteLine(
            "  --postgres-batch-size  The number of records to commit to the database at once. [Default=100000]");
    }

    public void Init(CommandLineParser cliArgs)
    {
        var connStrBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = cliArgs.GetValue("postgres-host") ?? "localhost",
            Port = Convert.ToInt32(cliArgs.GetValue("postgres-port") ?? "5432"),
            Username = cliArgs.GetValue("postgres-user"),
            Password = cliArgs.GetValue("postgres-pass"),
            Database = cliArgs.GetValue("postgres-db"),
            PersistSecurityInfo = true,
            IncludeErrorDetail = true,
            CommandTimeout = 0
        };

        _batchSize = Convert.ToInt32(cliArgs.GetValue("postgres-batch-size") ?? "100000");

        _dbContext = new StandardDbContext(new DbContextOptionsBuilder<StandardDbContext>()
            .LogTo(Console.WriteLine)
            .UseNpgsql(connStrBuilder.ToString())
            .UseSnakeCaseNamingConvention()
            .Options);
        if (Convert.ToBoolean(cliArgs.GetValue("postgres-replace") ?? "false"))
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();
        }

        _dbContext.Database.OpenConnection();
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