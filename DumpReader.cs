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

using System.Xml;
using LibArchive.Net;
using StackExchangeDumpConverter.Destinations;
using StackExchangeDumpConverter.Dto;

namespace StackExchangeDumpConverter;

public class DumpReader
{
    private HashSet<long> _knownPostIds = new();

    private readonly HashSet<long> _knownUserIds = new();

    public DumpReader(IDumpDestination destination, params string[] fileNames)
    {
        FileNames = fileNames;
        Destination = destination;
    }

    public string[] FileNames { get; }
    public IDumpDestination Destination { get; }

    private void AddDummyUser(long userId)
    {
        Destination.StoreUser(new User(userId,
            $"[Missing User ID {userId}]",
            -1,
            null,
            null,
            null,
            -1,
            -1,
            -1,
            -1,
            default,
            default));
    }

    private void AddDummyPost(long postId, short postTypeId)
    {
        Destination.StorePost(new Post(postId,
            postTypeId,
            null,
            null,
            null,
            null,
            null,
            null,
            -1,
            null,
            null,
            null,
            null,
            "Unknown",
            null,
            null,
            $"[Missing Post ID {postId}]",
            default,
            null,
            default,
            null,
            null));
    }

    public void ReadDump()
    {
        HashSet<long> addedDummyUsers = new();
        HashSet<long> addedDummyPosts = new();

        FindAndReadDumpFile<User>("Users.xml", u =>
        {
            _knownUserIds.Add(u.Id);
            Destination.StoreUser(u);
        });

        {
            List<Badge> deferredBadges = new();
            FindAndReadDumpFile<Badge>("Badges.xml", b =>
            {
                if (!_knownUserIds.Contains(b.UserId))
                {
                    if (addedDummyUsers.Add(b.UserId))
                        AddDummyUser(b.UserId);
                    deferredBadges.Add(b);
                }
                else
                {
                    Destination.StoreBadge(b);
                }
            });
            deferredBadges.ForEach(Destination.StoreBadge);
            Destination.Flush();
            _knownUserIds.UnionWith(addedDummyUsers);
            addedDummyUsers.Clear();
        }

        // Need to loop through Posts twice to be able to handle deleted posts. :(
        _knownPostIds = FindAndReadIdsFromFile("Posts.xml");

        {
            List<Post> deferredPosts = new();
            FindAndReadDumpFile<Post>("Posts.xml", p =>
            {
                var defer = false;
                // If we get an accepted answer pointing nowhere, add a dummy post.
                if (p.AcceptedAnswerId != null && !_knownPostIds.Contains(p.AcceptedAnswerId.Value))
                    if (addedDummyPosts.Add(p.AcceptedAnswerId.Value))
                        AddDummyPost(p.AcceptedAnswerId.Value, 2 /* Answer */);
                // Don't need to defer since it will be added before the current post anyway.
                // If we get an invalid owner user id, generate a dummy one.
                if (p.OwnerUserId != null && !_knownUserIds.Contains(p.OwnerUserId.Value))
                {
                    if (addedDummyUsers.Add(p.OwnerUserId.Value))
                        AddDummyUser(p.OwnerUserId.Value);
                    defer = true;
                }

                // If we get an invalid last edit user id, generate a dummy one.
                if (p.LastEditorUserId != null && !_knownUserIds.Contains(p.LastEditorUserId.Value))
                {
                    if (addedDummyUsers.Add(p.LastEditorUserId.Value))
                        AddDummyUser(p.LastEditorUserId.Value);
                    defer = true;
                }

                if (defer)
                    deferredPosts.Add(p);
                else
                    Destination.StorePost(p);
            });
            deferredPosts.ForEach(Destination.StorePost);
            Destination.Flush();
            _knownUserIds.UnionWith(addedDummyUsers);
            addedDummyUsers.Clear();
            _knownPostIds.UnionWith(addedDummyPosts);
            addedDummyPosts.Clear();
        }

        {
            List<PostHistory> deferredPostHistory = new();
            FindAndReadDumpFile<PostHistory>("PostHistory.xml", ph =>
            {
                if (!_knownPostIds.Contains(ph.PostId))
                {
                    if (addedDummyPosts.Add(ph.PostId))
                        AddDummyPost(ph.PostId, -1 /* Unknown */);
                    deferredPostHistory.Add(ph);
                }
                else
                {
                    Destination.StorePostHistory(ph);
                }
            });
            deferredPostHistory.ForEach(Destination.StorePostHistory);
            Destination.Flush();
            _knownPostIds.UnionWith(addedDummyPosts);
            addedDummyPosts.Clear();
        }

        FindAndReadDumpFile<Comment>("Comments.xml", Destination.StoreComment);

        {
            List<PostLink> deferredPostLinks = new();
            FindAndReadDumpFile<PostLink>("PostLinks.xml", pl =>
            {
                if (!_knownPostIds.Contains(pl.RelatedPostId))
                {
                    if (addedDummyPosts.Add(pl.RelatedPostId))
                        AddDummyPost(pl.RelatedPostId, -1 /* Unknown */);
                    deferredPostLinks.Add(pl);
                }
                else
                {
                    Destination.StorePostLink(pl);
                }
            });
            deferredPostLinks.ForEach(Destination.StorePostLink);
            Destination.Flush();
            _knownPostIds.UnionWith(addedDummyPosts);
            addedDummyPosts.Clear();
        }

        {
            List<Tag> deferredTags = new();
            FindAndReadDumpFile<Tag>("Tags.xml", t =>
            {
                var defer = false;

                if (t.ExcerptPostId != null && !_knownPostIds.Contains(t.ExcerptPostId.Value))
                {
                    if (addedDummyPosts.Add(t.ExcerptPostId.Value))
                        AddDummyPost(t.ExcerptPostId.Value, 4 /* Tag wiki excerpt */);
                    defer = true;
                }

                if (t.WikiPostId != null && !_knownPostIds.Contains(t.WikiPostId.Value))
                {
                    if (addedDummyPosts.Add(t.WikiPostId.Value))
                        AddDummyPost(t.WikiPostId.Value, 5 /* Tag wiki */);
                    defer = true;
                }

                if (defer)
                    deferredTags.Add(t);
                else
                    Destination.StoreTag(t);
            });
            deferredTags.ForEach(Destination.StoreTag);
            Destination.Flush();
            _knownPostIds.UnionWith(addedDummyPosts);
            addedDummyPosts.Clear();
        }

        {
            List<Vote> deferredVotes = new();
            FindAndReadDumpFile<Vote>("Votes.xml", v =>
            {
                if (!_knownPostIds.Contains(v.PostId))
                {
                    if (addedDummyPosts.Add(v.PostId))
                        AddDummyPost(v.PostId, -1 /* Unknown */);
                    deferredVotes.Add(v);
                }
                else
                {
                    Destination.StoreVote(v);
                }
            });
            deferredVotes.ForEach(Destination.StoreVote);
            Destination.Flush();
            _knownPostIds.UnionWith(addedDummyPosts);
            addedDummyPosts.Clear();
        }
    }

    private HashSet<long> FindAndReadIdsFromFile(string file)
    {
        foreach (var fileName in FileNames)
        {
            using var zipReader = new LibArchiveReader(fileName);

            var matchingFile = zipReader.Entries().FirstOrDefault(f => f.Name == file);
            if (matchingFile != null)
            {
                Console.WriteLine($"Reading IDs from {file}...");
                using var reader = XmlReader.Create(matchingFile.Stream);
                reader.MoveToContent();
                var ids = new HashSet<long>();
                while (reader.Read())
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "row") ids.Add(long.Parse(reader.GetAttribute("Id")!));
                            break;
                    }

                return ids;
            }
        }

        return [];
    }

    private void FindAndReadDumpFile<T>(string file, Action<T> action)
    {
        foreach (var fileName in FileNames)
        {
            using var zipReader = new LibArchiveReader(fileName);

            var matchingFile = zipReader.Entries().FirstOrDefault(f => f.Name == file);
            if (matchingFile != null)
            {
                Console.WriteLine($"Reading {file}...");
                try
                {
                    ReadDumpFile(matchingFile.Stream, action);
                }
                catch
                {
                    Console.WriteLine($"Failed to read {file}");
                    throw;
                }

                Destination.Flush();
                return;
            }
        }
    }

    private void ReadDumpFile<T>(Stream fileStream, Action<T> callback)
    {
        using var reader = XmlReader.Create(fileStream);
        reader.MoveToContent();

        var constructor = typeof(T).GetConstructors().First();
        var resolvedParams = constructor.GetParameters()
            .Select(p => new
            {
                Name = p.Name!,
                Type = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType
            })
            .ToList();

        while (reader.Read())
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (reader.Name == "row")
                    {
                        List<object?> loadedData = new();
                        foreach (var parameter in resolvedParams)
                        {
                            var realType = parameter.Type;
                            var value = reader.GetAttribute(parameter.Name);
                            if (value == null)
                            {
                                loadedData.Add(null);
                            }
                            else if (realType == typeof(DateTime))
                            {
                                var datetime = Convert.ToDateTime(value);
                                datetime = DateTime.SpecifyKind(datetime, DateTimeKind.Utc);
                                loadedData.Add(datetime);
                            }
                            else if (realType == typeof(string[]))
                            {
                                loadedData.Add(value
                                    .Split('|')
                                    .Where(s => s.Length > 0)
                                    .ToArray());
                            }
                            else
                            {
                                loadedData.Add(Convert.ChangeType(value, realType));
                            }
                        }

                        callback((T) constructor.Invoke(loadedData.ToArray()));
                    }

                    break;
            }
    }
}