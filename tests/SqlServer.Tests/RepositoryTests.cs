using System.Threading.Tasks;
using Inkslab.Linq;
using Xunit;

namespace SqlServer.Tests
{
    public class RepositoryTests
    {
        private readonly IRepository<Activity> _repository;

        public RepositoryTests(IRepository<Activity> repository)
        {
            _repository = repository;
        }

        [Theory]
        [InlineData(1, "New Activity Name 1")]
        public async Task UpdateActivityNameAsync(long id, string newName)
        {
            await _repository
                .Where(a => a.Id == id)
                .UpdateAsync(a => new Activity
                {
                    Name = newName
                });
        }

        [Fact]
        public async Task UpdateAllActivitiesAsync()
        {
            await _repository
                .UpdateAsync(a => new Activity
                {
                    CreateTime = a.CreateTime.AddHours(2)
                });
        }

        [Fact]
        public async Task DeleteAllActivitiesAsync()
        {
            using (var tran = new Inkslab.Transactions.TransactionUnit())
            {
                await _repository.DeleteAsync();
            }
        }

        [Fact]
        public async Task DeleteActivityByIdAsync()
        {
            await _repository
                .Where(a => a.Id == 1)
                .DeleteAsync();
        }
    }
}