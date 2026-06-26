using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Post.Application.Interfaces;

namespace Post.Application.Services
{
    /// <summary>
    /// Tách @username trong nội dung post/comment và bắn event thông báo tới
    /// những người được nhắc (qua RabbitMQ → Notification service).
    /// </summary>
    public static class MentionHelper
    {
        private static readonly Regex MentionRegex =
            new(@"@([A-Za-z0-9_.]+)", RegexOptions.Compiled);

        public static List<string> ExtractUsernames(string? content)
        {
            if (string.IsNullOrEmpty(content)) return new List<string>();
            return MentionRegex.Matches(content)
                .Select(m => m.Groups[1].Value)
                .Where(u => u.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static async Task PublishMentionsAsync(
            string? content,
            Guid postId,
            Guid authorId,
            bool isComment,
            IUserProfileHttpClient userClient,
            IMessagePublisher publisher)
        {
            var usernames = ExtractUsernames(content);
            if (usernames.Count == 0) return;

            var notified = new HashSet<Guid>();
            foreach (var username in usernames)
            {
                var userId = await userClient.GetUserIdByUsernameAsync(username);
                if (userId == null || userId == authorId) continue;
                if (!notified.Add(userId.Value)) continue; // tránh trùng

                await publisher.PublishUserMentionedAsync(postId, authorId, userId.Value, isComment);
            }
        }
    }
}
