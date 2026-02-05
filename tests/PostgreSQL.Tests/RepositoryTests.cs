using System;
using System.Linq;
using System.Threading.Tasks;
using Inkslab.Linq;
using Xunit;
using XunitPlus;

namespace PostgreSQL.Tests
{
    /// <summary>
    /// 查询测试。
    /// </summary>
    [TestPriority(60)]
    public class RepositoryTests
    {
        private readonly IRepository<UserContentsOfJsonbPayload> _repositoryOfJsonbPayload;
        private readonly IRepository<DeliverySnt> _repositoryOfDeliverySnt;
        private readonly IQueryable<DeliverySnt> _queryableOfDeliverySnt;
        private readonly IQueryable<UserContentsOfJsonbPayload> _queryableOfJsonbPayload;
        private readonly IRepository<User> _repositoryOfUser;
        private readonly IQueryable<User> _queryableOfUser;

        public RepositoryTests(
            IRepository<UserContentsOfJsonbPayload> repositoryOfJsonbPayload,
            IRepository<DeliverySnt> repositoryOfDeliverySnt,
            IQueryable<DeliverySnt> queryableOfDeliverySnt,
            IQueryable<UserContentsOfJsonbPayload> queryableOfJsonbPayload,
            IRepository<User> repositoryOfUser,
            IQueryable<User> queryableOfUser)
        {
            _repositoryOfJsonbPayload = repositoryOfJsonbPayload;
            _repositoryOfDeliverySnt = repositoryOfDeliverySnt;
            _queryableOfDeliverySnt = queryableOfDeliverySnt;
            _queryableOfJsonbPayload = queryableOfJsonbPayload;
            _repositoryOfUser = repositoryOfUser;
            _queryableOfUser = queryableOfUser;
        }

        [Fact]
        public async Task TestJsonbPayloadAsync()
        {
            await _repositoryOfJsonbPayload.Into(new UserContentsOfJsonbPayload
            {
                Content = "{\"name\":\"inkslab\",\"age\":35}"
            })
            .ExecuteAsync();

            var result = await _queryableOfJsonbPayload.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        }

        [Fact]
        public async Task TestInsertIgnoreAsync()
        {
            int result = await _repositoryOfUser.Ignore().InsertAsync(_queryableOfUser.OrderBy(x => x.Id).Take(1));
        }

        [Fact]
        public async Task TestUpdateAsync()
        {
            await _repositoryOfUser.Where(u => u.Id == 1)
                 .UpdateAsync(u => new User
                 {
                     Name = "Updated Name",
                     Age = 30
                 });
        }

        [Fact]
        public async Task TestUpdateAllAsync()
        {
            await _repositoryOfUser
                 .UpdateAsync(u => new User
                 {
                     Name = "Updated Name",
                     Age = u.Age + 1
                 });
        }

        [Fact]
        public async Task TestDeleteAsync()
        {
            await _repositoryOfUser.Where(u => u.Id == 1).DeleteAsync();
        }

        [Fact]
        public async Task TestDeleteAllAsync()
        {
            await _repositoryOfUser.DeleteAsync();
        }

        [Fact]
        public async Task TestDeliverySntAsync()
        {
            DeliverySnt deliverySnt = new()
            {
                DeliveryId = 1000001,
                DataSourceId = 1000001,
                RequestContent = "{\"Id\": 1000000001, \"Sex\": 1, \"Name\": \"string\", \"Address\": {\"City\": \"string\", \"Street\": \"string\"}}",
                ResponseContent = string.Empty,
                Added = DateTime.Now
            };

            await _repositoryOfDeliverySnt
                .DataSharding("202512")
                .Into(deliverySnt)
                .ExecuteAsync();
        }

        [Fact]
        public async Task TestDeliverySntUpdateAsync()
        {
            var result = await _repositoryOfDeliverySnt
                .DataSharding("202512")
                .Where(x => x.Id == 6900000000000000000)
                .UpdateAsync(x => new DeliverySnt
                {
                    RequestContent = "{\"Code\":200,\"Message\":\"Success\"}",
                    Duration = DateTime.Now.Ticks
                });
        }

        [Fact]
        public async Task TestDeliverySntQueryAsync()
        {
            var result = await _queryableOfDeliverySnt
                .DataSharding("202512")
                .Where(x => x.Id == 6900000000000000000)
                .OrderBy(x => x.Id)
                .Select(x => x.RequestContent.ToString())
                .FirstOrDefaultAsync();
        }
    }
}