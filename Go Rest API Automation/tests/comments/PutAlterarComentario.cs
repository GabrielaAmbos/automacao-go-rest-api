using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests.comments
{
    [TestFixture]
    public class PutAlterarComentario
    {
        private User user;
        private Post post;
        private Comment comment;

        [OneTimeSetUp]
        public void CriarPost()
        {
            user = UserFactory.Create("Octavia Butler");
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
            comment = CommentFactory.Create(post.Id, "Ada Lovelace", "Comentario original.");
        }

        [TearDown]
        public void ExcluirComentario()
        {
            CommentFactory.Delete(comment.Id);
        }

        [Test]
        public void DeveAlterarComentarioExistente()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("body", "Comentario editado.");

            var response = ApiClient<Comment>.Request(
                EndpointProvider.Comment(comment.Id), Method.PUT, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Id.Should().Be(comment.Id);
            response.Data.Body.Should().Be("Comentario editado.");
            // name nao foi enviado no corpo, a API deve preservar o valor original
            response.Data.Name.Should().Be("Ada Lovelace");
        }

        [Test]
        public void NaoDeveAlterarComentarioInexistente()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("body", "Comentario editado.");

            var response = ApiClient<ErrorMessage>.Request(
                EndpointProvider.Comment(-comment.Id), Method.PUT, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
