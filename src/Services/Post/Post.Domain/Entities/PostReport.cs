using System;

namespace Post.Domain.Entities
{
    public enum ReportStatus
    {
        Pending     = 0,  // chờ admin xử lý
        Dismissed   = 1,  // admin bỏ qua (không vi phạm)
        ActionTaken = 2   // admin đã xử lý (ẩn bài)
    }

    /// <summary>A user's report of a post. Reviewed by an admin.</summary>
    public class PostReport
    {
        public Guid Id { get; private set; }
        public Guid PostId { get; private set; }
        public Guid ReporterId { get; private set; }
        public string Reason { get; private set; }
        public ReportStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ReviewedAt { get; private set; }
        public Guid? ReviewedBy { get; private set; }

        private PostReport() { Reason = null!; }

        public static PostReport Create(Guid postId, Guid reporterId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                reason = "Khác";
            if (reason.Length > 500)
                reason = reason.Substring(0, 500);

            return new PostReport
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                ReporterId = reporterId,
                Reason = reason.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Resolve(ReportStatus status, Guid adminId)
        {
            Status = status;
            ReviewedBy = adminId;
            ReviewedAt = DateTime.UtcNow;
        }
    }
}
