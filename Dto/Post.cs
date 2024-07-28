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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StackExchangeDumpConverter.Dto;

public record Post(
    [property: Key] long Id,
    [property: ForeignKey(nameof(PostType))]
    short PostTypeId,
    [property: ForeignKey(nameof(Post))] long? ParentId,
    [property: ForeignKey(nameof(Post))] long? AcceptedAnswerId,
    [property: ForeignKey(nameof(User))] long? OwnerUserId,
    [property: MaxLength(40)] string? OwnerDisplayName,
    [property: ForeignKey(nameof(User))] long? LastEditorUserId,
    [property: MaxLength(40)] string? LastEditorDisplayName,
    int Score,
    int? ViewCount,
    int? AnswerCount,
    int? CommentCount,
    int? FavoriteCount,
    [property: MaxLength(30)] string ContentLicense,
    string[]? Tags,
    [property: MaxLength(250)] string? Title,
    string Body,
    DateTime CreationDate,
    DateTime? LastEditDate,
    DateTime LastActivityDate,
    DateTime? ClosedDate,
    DateTime? CommunityOwnedDate);