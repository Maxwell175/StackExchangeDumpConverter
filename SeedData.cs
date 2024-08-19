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

using StackExchangeDumpConverter.Dto;

namespace StackExchangeDumpConverter;

// Based on: https://meta.stackexchange.com/questions/2677/database-schema-documentation-for-the-public-data-dump-and-sede
public static class SeedData
{
    public static BadgeClass[] BadgeClasses =
    [
        new BadgeClass(1, "Gold"),
        new BadgeClass(2, "Silver"),
        new BadgeClass(3, "Bronze")
    ];

    public static PostType[] PostTypes =
    [
        new PostType(-1, "Unknown"),
        new PostType(1, "Question"),
        new PostType(2, "Answer"),
        new PostType(3, "Orphaned tag wiki"),
        new PostType(4, "Tag wiki excerpt"),
        new PostType(5, "Tag wiki"),
        new PostType(6, "Moderator nomination"),
        new PostType(7, "Wiki placeholder"),
        new PostType(8, "Privilege wiki")
    ];

    public static PostHistoryType[] PostHistoryTypes =
    [
        new PostHistoryType(1, "Initial Title - initial title (questions only)"),
        new PostHistoryType(2, "Initial Body - initial post raw body text"),
        new PostHistoryType(3, "Initial Tags - initial list of tags (questions only)"),
        new PostHistoryType(4, "Edit Title - modified title (questions only)"),
        new PostHistoryType(5, "Edit Body - modified post body (raw markdown)"),
        new PostHistoryType(6, "Edit Tags - modified list of tags (questions only)"),
        new PostHistoryType(7, "Rollback Title - reverted title (questions only)"),
        new PostHistoryType(8, "Rollback Body - reverted body (raw markdown)"),
        new PostHistoryType(9, "Rollback Tags - reverted list of tags (questions only)"),
        new PostHistoryType(10, "Post Closed - post voted to be closed"),
        new PostHistoryType(11, "Post Reopened - post voted to be reopened"),
        new PostHistoryType(12, "Post Deleted - post voted to be removed"),
        new PostHistoryType(13, "Post Undeleted - post voted to be restored"),
        new PostHistoryType(14, "Post Locked - post locked by moderator"),
        new PostHistoryType(15, "Post Unlocked - post unlocked by moderator"),
        new PostHistoryType(16, "Community Owned - post now community owned"),
        new PostHistoryType(17, "Post Migrated - post migrated - now replaced by 35/36 (away/here)"),
        new PostHistoryType(18, "Question Merged - question merged with deleted question"),
        new PostHistoryType(19, "Question Protected - question was protected by a moderator."),
        new PostHistoryType(20, "Question Unprotected - question was unprotected by a moderator."),
        new PostHistoryType(21, "Post Disassociated - OwnerUserId removed from post by admin"),
        new PostHistoryType(22, "Question Unmerged - answers/votes restored to previously merged question"),
        new PostHistoryType(24, "Suggested Edit Applied"),
        new PostHistoryType(25, "Post Tweeted"),
        new PostHistoryType(31, "Comment discussion moved to chat"),
        new PostHistoryType(33, "Post notice added - comment contains foreign key to PostNotices"),
        new PostHistoryType(34, "Post notice removed - comment contains foreign key to PostNotices"),
        new PostHistoryType(35, "Post migrated away - replaces id 17"),
        new PostHistoryType(36, "Post migrated here - replaces id 17"),
        new PostHistoryType(37, "Post merge source"),
        new PostHistoryType(38, "Post merge destination"),
        new PostHistoryType(50, "Bumped by Community User"),
        new PostHistoryType(52, "Question became hot network question (main) / Hot Meta question (meta)"),
        new PostHistoryType(53, "Question removed from hot network/meta questions by a moderator"),
        new PostHistoryType(66, "Created from Ask Wizard"),
        new PostHistoryType(23, "Unknown dev related event"),
        new PostHistoryType(26, "Vote nullification by dev (ERM?)"),
        new PostHistoryType(27, "Post unmigrated/hidden moderator migration?"),
        new PostHistoryType(28, "Unknown suggestion event"),
        new PostHistoryType(29, "Unknown moderator event (possibly de-wikification?)"),
        new PostHistoryType(30, "Unknown event (too rare to guess)")
    ];

    public static LinkType[] LinkTypes =
    [
        new LinkType(1, "Linked"),
        new LinkType(3, "Duplicate")
    ];

    public static VoteType[] VoteTypes =
    [
        new VoteType(1, "AcceptedByOriginator"),
        new VoteType(2, "UpMod"),
        new VoteType(3, "DownMod"),
        new VoteType(4, "Offensive"),
        new VoteType(5, "Bookmark"),
        new VoteType(6, "Close"),
        new VoteType(7, "Reopen"),
        new VoteType(8, "BountyStart"),
        new VoteType(9, "BountyClose"),
        new VoteType(10, "Deletion"),
        new VoteType(11, "Undeletion"),
        new VoteType(12, "Spam"),
        new VoteType(13, "Unknown"),
        new VoteType(14, "NominateModerator"),
        new VoteType(15, "ModeratorReview"),
        new VoteType(16, "ApproveEditSuggestion"),
        new VoteType(17, "Reaction1"),
        new VoteType(18, "Helpful"),
        new VoteType(19, "ThankYou"),
        new VoteType(20, "WellWritten"),
        new VoteType(21, "Follow"),
        new VoteType(22, "Reaction2"),
        new VoteType(23, "Reaction3"),
        new VoteType(24, "Reaction4"),
        new VoteType(25, "Reaction5"),
        new VoteType(26, "Reaction6"),
        new VoteType(27, "Reaction7"),
        new VoteType(28, "Reaction8"),
        new VoteType(29, "Outdated"),
        new VoteType(30, "NotOutdated"),
        new VoteType(31, "PreVote"),
        new VoteType(32, "CollectiveDiscussionUpvote"),
        new VoteType(33, "CollectiveDiscussionDownvote")
    ];
}