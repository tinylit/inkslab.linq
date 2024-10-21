﻿using Inkslab.Transcations;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Linq.Tests
{
    public class TransactionTests
    {
        private readonly IRepository<User> _userRpts;
        private readonly IQueryable<User> _users;

        public TransactionTests(IRepository<User> userRpts, IQueryable<User> users)
        {
            _userRpts = userRpts;
            _users = users;
        }

        [Fact]
        public async Task RollbackAsync()
        {
            await using (var transaction = new TransactionUnit())
            {
                await _userRpts.UpdateAsync(x => new User
                {
                    Date = DateTime.Now
                });
            }
        }

        [Fact]
        public async Task CommitAsync()
        {
            await using (var transaction = new TransactionUnit())
            {
                await _userRpts.UpdateAsync(x => new User
                {
                    Date = DateTime.Now
                });

                await transaction.CompleteAsync();
            }
        }

        [Fact]
        public async Task ReadonlyAsync()
        {
            await using (var transaction = new TransactionUnit())
            {
                await _users.OrderBy(x => x.Id).FirstOrDefaultAsync();

                //? 不会有事务。
                await transaction.CompleteAsync();
            }
        }
    }
}
