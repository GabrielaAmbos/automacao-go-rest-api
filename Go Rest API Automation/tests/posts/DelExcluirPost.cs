using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests.posts
{
    [TestFixture]
    public class DelExcluirPost
    {
        private User user;
        private Post post;

        [OneTimeSetUp]
        public void CriarUsuario()
        {
            user = UserFactory.Create("Rachel de Queiroz");
        }

        [OneTimeTearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(user.Id);
        }

        [SetUp]
        public void CriarPost()
        {
            post = PostFactory.Create(user.Id);
        }

        [TearDown]
        public void ExcluirPost()
        {
            PostFactory.Delete(post.Id);
        }

        [Test]
        public void DeveExcluirPostExistente()
        {
            var response = ApiClient<Post>.Request(EndpointProvider.Post(post.Id), Method.DELETE);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.Data.Should().BeNull();
        }

        [Test]
        public void NaoDeveExcluirPostInexistente()
        {
            var response = ApiClient<ErrorMessage>.Request(
                EndpointProvider.Post(-post.Id), Method.DELETE);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
