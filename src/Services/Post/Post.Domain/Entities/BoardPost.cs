using System;

namespace Post.Domain.Entities
{
    public enum BoardTag
    {
        HoiBai    = 0,  // hỏibài
        Timeline  = 1,  // timeline
        TimPhong  = 2,  // tìmphòng
        TamSu     = 3,  // tâmsự
        SaleDo    = 4   // saleđồ
    }

    public class BoardPost
    {
        public Guid Id { get; private set; }
        public Guid? AuthorId { get; private set; }   // null nếu ẩn danh
        public BoardTag Tag { get; private set; }
        public string Content { get; private set; }
        public int UpvotesCount { get; private set; }
        public int DownvotesCount { get; private set; }
        public int CommentsCount { get; private set; }
        public bool IsAnonymous { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        private BoardPost() { }

        public static BoardPost Create(Guid? authorId, BoardTag tag, string content, bool isAnonymous)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty");
            if (content.Length > 2000)
                throw new ArgumentException("Content cannot exceed 2000 characters");

            return new BoardPost
            {
                Id = Guid.NewGuid(),
                AuthorId = isAnonymous ? null : authorId,
                Tag = tag,
                Content = content.Trim(),
                IsAnonymous = isAnonymous,
                UpvotesCount = 0,
                DownvotesCount = 0,
                CommentsCount = 0,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public void IncrementUpvotes()   => UpvotesCount++;
        public void DecrementUpvotes()   { if (UpvotesCount > 0) UpvotesCount--; }
        public void IncrementDownvotes() => DownvotesCount++;
        public void DecrementDownvotes() { if (DownvotesCount > 0) DownvotesCount--; }
        public void IncrementComments()  => CommentsCount++;
        public void DecrementComments()  { if (CommentsCount > 0) CommentsCount--; }

        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        // Score dùng cho sort "Hot": net votes + comments có trọng số, decay theo thời gian
        public double HotScore
        {
            get
            {
                var net = UpvotesCount - DownvotesCount + CommentsCount * 2;
                var hours = (DateTime.UtcNow - CreatedAt).TotalHours + 2;
                return net / Math.Pow(hours, 1.5);
            }
        }

        public bool CanBeDeletedBy(Guid userId) => AuthorId == userId && !IsDeleted;
    }
}
