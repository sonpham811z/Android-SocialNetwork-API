using System;

namespace Post.Domain.Entities
{
    public class StoryView
    {
        public Guid Id { get; private set; }
        public Guid StoryId { get; private set; }
        public Guid ViewerId { get; private set; }
        public DateTime ViewedAt { get; private set; }

        public Story? Story { get; private set; }

        private StoryView() { }

        public static StoryView Create(Guid storyId, Guid viewerId)
        {
            return new StoryView
            {
                Id = Guid.NewGuid(),
                StoryId = storyId,
                ViewerId = viewerId,
                ViewedAt = DateTime.UtcNow
            };
        }
    }
}
