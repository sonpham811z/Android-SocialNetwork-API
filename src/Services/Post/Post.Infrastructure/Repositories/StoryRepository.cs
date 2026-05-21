using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Post.Domain.Entities;
using Post.Domain.Interfaces;
using Post.Infrastructure.Data;

namespace Post.Infrastructure.Repositories
{
    public class StoryRepository : IStoryRepository
    {
        private readonly PostDbContext _context;

        public StoryRepository(PostDbContext context)
        {
            _context = context;
        }

        public async Task<Story?> GetByIdAsync(Guid id)
        {
            return await _context.Stories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Story?> GetByIdWithViewsAsync(Guid id)
        {
            return await _context.Stories
                .IgnoreQueryFilters()
                .Include(s => s.Views)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Story>> GetUserActiveStoriesAsync(Guid userId)
        {
            return await _context.Stories
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Story>> GetFeedStoriesAsync(List<Guid> userIds)
        {
            return await _context.Stories
                .Include(s => s.Views)
                .Where(s => userIds.Contains(s.UserId))
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasUserViewedStoryAsync(Guid storyId, Guid viewerId)
        {
            return await _context.StoryViews
                .AnyAsync(v => v.StoryId == storyId && v.ViewerId == viewerId);
        }

        public async Task<Story> AddAsync(Story story)
        {
            await _context.Stories.AddAsync(story);
            return story;
        }

        public async Task UpdateAsync(Story story)
        {
            _context.Stories.Update(story);
            await Task.CompletedTask;
        }

        public async Task<StoryView> AddViewAsync(StoryView view)
        {
            await _context.StoryViews.AddAsync(view);
            return view;
        }
    }
}
