using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;
using User.Application.Interfaces;

namespace User.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly UserDbContext _context;
        private IDbContextTransaction _currentTransaction;

        public UnitOfWork(UserDbContext context)
        {
            _context = context;

        }

        public async Task BeginTransactionAsync()
        {
            if(_currentTransaction != null)
                return;
            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync(); // save data
                await _currentTransaction.CommitAsync(); //commit
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }
        public async Task RollbackTransactionAsync()
        {
            try
            {
                if(_currentTransaction != null)
                    await _currentTransaction.RollbackAsync();
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}