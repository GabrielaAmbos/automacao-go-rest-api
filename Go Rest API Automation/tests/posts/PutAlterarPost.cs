using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests.posts
{
    [TestFixture]
    public class PutAlterarPost
    {
        private User user;
        private Post post;

        [OneTimeSetUp]
        public void CriarUsuario()
        {
            user = UserFactory.Create("Graciliano Ramos");
        }

        [OneTimeTearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(user.Id);
        }

        [SetUp]
        public void CriarPost()
        {
            post = PostFactory.Create(user.Id, "Vidas Secas", "Na planicie avermelhada.");
        }

        [TearDown]
        public void ExcluirPost()
        {
            PostFactory.Delete(post.Id);
        }

        [Test]
        public void DeveAlterarPostExistente()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("title", "Vidas Secas - edicao revisada");

            var response = ApiClient<Post>.Request(EndpointProvider.Post(post.Id), Method.PUT, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Id.Should().Be(post.Id);
            response.Data.Title.Should().Be("Vidas Secas - edicao revisada");
            // body nao foi enviado no corpo, a API deve preservar o valor original
            response.Data.Body.Should().Be("Na planicie avermelhada.");
        }

        [Test]
        public void NaoDeveAlterarPostInexistente()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("title", "Titulo qualquer");

            var response = ApiClient<ErrorMessage>.Request(
                EndpointProvider.Post(-post.Id), Method.PUT, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
