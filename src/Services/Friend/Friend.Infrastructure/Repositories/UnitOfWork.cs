using System;
using System.Threading.Tasks;
using Friend.Domain.Interfaces;
using Friend.Infrastructure.Data;
using Friend.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Friend.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FriendDbContext _ctx;
        private IDbContextTransaction? _transaction;

        public IFriendshipRepository Friendships { get; }
        public IFriendRequestRepository FriendRequests { get; }
        public IFollowRepository Follows { get; }
        public IBlockRepository Blocks { get; }

        public UnitOfWork(FriendDbContext ctx)
        {
            _ctx = ctx;
            Friendships  = new FriendshipRepository(ctx);
            FriendRequests = new FriendRequestRepository(ctx);
            Follows      = new FollowRepository(ctx);
            Blocks       = new BlockRepository(ctx);
        }

        public async Task SaveChangesAsync() => await _ctx.SaveChangesAsync();

        public async Task BeginTransactionAsync() =>
            _transaction = await _ctx.Database.BeginTransactionAsync();

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
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
            _ctx.Dispose();
        }
    }
}