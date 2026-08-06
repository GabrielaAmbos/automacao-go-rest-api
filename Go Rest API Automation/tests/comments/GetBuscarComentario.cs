using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;

namespace Go_Rest_API_Automation.tests.comments
{
    [TestFixture]
    public class GetBuscarComentario
    {
        private User user;
        private Post post;
        private Comment comment;

        [OneTimeSetUp]
        public void CriarComentario()
        {
            user = UserFactory.Create("Carl Sagan");
            post = PostFactory.Create(user.Id);
            comment = CommentFactory.Create(post.Id, "Ada Lovelace", "Excelente leitura.");
        }

        [OneTimeTearDown]
        public void ExcluirComentario()
        {
            CommentFactory.Delete(comment.Id);
            PostFactory.Delete(post.Id);
            UserFactory.Delete(user.Id);
        }

        [Test]
        public void DeveBuscarComentario()
        {
            var response = ApiClient<Comment>.Request(EndpointProvider.Comment(comment.Id));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Id.Should().Be(comment.Id);
            response.Data.PostId.Should().Be(post.Id);
            response.Data.Name.Should().Be("Ada Lovelace");
            response.Data.Body.Should().Be("Excelente leitura.");
            response.Data.Email.Should().Be(comment.Email);
        }

        [Test]
        public void NaoDeveBuscarComentario()
        {
            var response = ApiClient<ErrorMessage>.Request(EndpointProvider.Comment(-comment.Id));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
