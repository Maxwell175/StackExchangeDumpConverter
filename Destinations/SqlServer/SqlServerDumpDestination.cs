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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StackExchangeDumpConverter.Destinations.EFCoreCommon;
using StackExchangeDumpConverter.Dto;

namespace StackExchangeDumpConverter.Destinations.SqlServer;

public class SqlServerDumpDestination : IDumpDestination
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
        Console.WriteLine("Sql Server Destination Options:");
        Console.WriteLine("  --sqlserver-datasource  The Sql Server to connect to. [Default=tcp:127.0.0.1]");
        Console.WriteLine("  --sqlserver-trustcert   Ignore SSL certificate errors. [Default=false]");
        Console.WriteLine("  --sqlserver-user        The username to authenticate with.");
        Console.WriteLine("  --sqlserver-pass        The password to authenticate with.");
        Console.WriteLine("  --sqlserver-db          The database that the data will be loaded into.");
        Console.WriteLine("  --sqlserver-replace     If true, all existing tables will be dropped and recreated.");
        Console.WriteLine(
            "                          If false, the database should already be created with a compatible schema. [Default=false]");
        Console.WriteLine(
            "  --sqlserver-batch-size  The number of records to commit to the database at once. [Default=100000]");
    }

    public void Init(CommandLineParser cliArgs)
    {
        var connStrBuilder = new SqlConnectionStringBuilder
        {
            DataSource = cliArgs.GetValue("sqlserver-host") ?? "localhost",
            UserID = cliArgs.GetValue("sqlserver-user"),
            Password = cliArgs.GetValue("sqlserver-pass"),
            InitialCatalog = cliArgs.GetValue("sqlserver-db"),
            TrustServerCertificate = Convert.ToBoolean(cliArgs.GetValue("sqlserver-trustcert") ?? "false"),
            PersistSecurityInfo = true,
            CommandTimeout = 0
        };

        _batchSize = Convert.ToInt32(cliArgs.GetValue("sqlserver-batch-size") ?? "100000");

        _dbContext = new StandardDbContext(new DbContextOptionsBuilder<StandardDbContext>()
            .LogTo(Console.WriteLine)
            .UseSqlServer(connStrBuilder.ToString())
            .Options);
        if (Convert.ToBoolean(cliArgs.GetValue("sqlserver-replace") ?? "false"))
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