using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Post.Domain.Interfaces;
using Post.Infrastructure.Data;

namespace Post.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PostDbContext _context;
        private IDbContextTransaction? _transaction;
        
        public IPostRepository Posts { get; }
        public ICommentRepository Comments { get; }
        public IPostLikeRepository PostLikes { get; }

        public UnitOfWork(PostDbContext context)
        {
            _context = context;
            Posts = new PostRepository(_context);
            Comments = new CommentRepository(_context);
            PostLikes = new PostLikeRepository(_context);

        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task OpenConnectionAsync()
        {
            await _context.Database.OpenConnectionAsync();
        }

        public async Task CloseConnectionAsync()
        {
            await _context.Database.GetDbConnection().CloseAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}