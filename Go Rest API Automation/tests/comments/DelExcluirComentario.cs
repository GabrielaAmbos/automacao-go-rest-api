using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests.comments
{
    [TestFixture]
    public class DelExcluirComentario
    {
        private User user;
        private Post post;
        private Comment comment;

        [OneTimeSetUp]
        public void CriarPost()
        {
            user = UserFactory.Create("Mary Shelley");
            post = PostFactory.Create(user.Id);
        }

        [OneTimeTearDown]
        public void ExcluirPost()
        {
            PostFactory.Delete(post.Id);
            UserFactory.Delete(user.Id);
        }

        [SetUp]
        public void CriarComentario()
        {
            comment = CommentFactory.Create(post.Id);
        }

        [TearDown]
        public void ExcluirComentario()
        {
            CommentFactory.Delete(comment.Id);
        }

        [Test]
        public void DeveExcluirComentarioExistente()
        {
            var response = ApiClient<Comment>.Request(EndpointProvider.Comment(comment.Id), Method.DELETE);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.Data.Should().BeNull();
        }

        [Test]
        public void NaoDeveExcluirComentarioInexistente()
        {
            var response = ApiClient<ErrorMessage>.Request(
                EndpointProvider.Comment(-comment.Id), Method.DELETE);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
