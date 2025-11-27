using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Inkslab.Linq;
using Inkslab.Linq.Annotations;
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

        public RepositoryTests(IRepository<UserContentsOfJsonDocument> repositoryOfJsonDocument,
            IRepository<UserContentsOfJObject> repositoryOfJObject,
            IRepository<UserContentsOfJsonbPayload> repositoryOfJsonbPayload,
            IQueryable<UserContentsOfJsonDocument> queryableOfJsonDocument,
            IQueryable<UserContentsOfJObject> queryableOfJObject,
            IQueryable<UserContentsOfJsonbPayload> queryableOfJsonbPayload)
        {
            _repositoryOfJsonDocument = repositoryOfJsonDocument;
            _repositoryOfJObject = repositoryOfJObject;
            _repositoryOfJsonbPayload = repositoryOfJsonbPayload;
            _queryableOfJsonDocument = queryableOfJsonDocument;
            _queryableOfJObject = queryableOfJObject;
            _queryableOfJsonbPayload = queryableOfJsonbPayload;
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
    }
}