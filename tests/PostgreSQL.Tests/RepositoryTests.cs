using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Inkslab.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PostgreSQL.Tests
{
    /// <summary>
    /// 查询测试。
    /// </summary>
    public class RepositoryTests
    {
        private readonly IRepository<UserContentsOfJsonDocument> _repositoryOfJsonDocument;
        private readonly IRepository<UserContentsOfJObject> _repositoryOfJObject;
        private readonly IRepository<UserContentsOfJsonbPayload> _repositoryOfJsonbPayload;
        private readonly IQueryable<UserContentsOfJsonDocument> _queryableOfJsonDocument;
        private readonly IQueryable<UserContentsOfJObject> _queryableOfJObject;
        private readonly IQueryable<UserContentsOfJsonbPayload> _queryableOfJsonbPayload;
        private readonly IRepository<User> _repositoryOfUser;
        private readonly IQueryable<User> _queryableOfUser;

        public RepositoryTests(IRepository<UserContentsOfJsonDocument> repositoryOfJsonDocument,
            IRepository<UserContentsOfJObject> repositoryOfJObject,
            IRepository<UserContentsOfJsonbPayload> repositoryOfJsonbPayload,
            IQueryable<UserContentsOfJsonDocument> queryableOfJsonDocument,
            IQueryable<UserContentsOfJObject> queryableOfJObject,
            IQueryable<UserContentsOfJsonbPayload> queryableOfJsonbPayload,
            IRepository<User> repositoryOfUser,
            IQueryable<User> queryableOfUser)
        {
            _repositoryOfJsonDocument = repositoryOfJsonDocument;
            _repositoryOfJObject = repositoryOfJObject;
            _repositoryOfJsonbPayload = repositoryOfJsonbPayload;
            _queryableOfJsonDocument = queryableOfJsonDocument;
            _queryableOfJObject = queryableOfJObject;
            _queryableOfJsonbPayload = queryableOfJsonbPayload;
            _repositoryOfUser = repositoryOfUser;
            _queryableOfUser = queryableOfUser;
        }

        [Fact]
        public async Task TestJsonDocumentAsync()
        {
            await _repositoryOfJsonDocument.Into(new UserContentsOfJsonDocument
            {
                Content = JsonDocument.Parse("{\"name\":\"inkslab\",\"age\":18}")
            })
            .ExecuteAsync();

            var result = await _queryableOfJsonDocument.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        }

        [Fact]
        public async Task TestJObjectAsync()
        {
            await _repositoryOfJObject.Into(new UserContentsOfJObject
            {
                Content = JObject.Parse("{\"name\":\"inkslab\",\"age\":20}")
            })
            .ExecuteAsync();

            var result = await _queryableOfJObject.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        }

        [Fact]
        public async Task TestJsonbPayloadAsync()
        {
            await _repositoryOfJsonbPayload.Into(new UserContentsOfJsonbPayload
            {
                Content = new JsonbPayload("{\"name\":\"inkslab\",\"age\":35}")
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
    }
}