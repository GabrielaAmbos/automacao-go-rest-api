using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests
{
    [TestFixture]
    public class PutAlterarUsuario
    {
        private User user;

        [SetUp]
        public void CriarUsuario()
        {
            user = UserFactory.Create("Ronald McDonald");
        }

        [TearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(user.Id);
        }

        [Test]
        public void DeveAlterarUsuarioExistente()
        {
            string email = UserFactory.UniqueEmail("Ronald McDonald");

            JObject jObjectBody = new JObject();
            jObjectBody.Add("name", "Ronald McDonald Jr");
            jObjectBody.Add("email", email);
            jObjectBody.Add("status", "inactive");

            var response = ApiClient<User>.Request(EndpointProvider.User(user.Id), Method.PUT, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Id.Should().Be(user.Id);
            response.Data.Name.Should().Be("Ronald McDonald Jr");
            response.Data.Email.Should().Be(email);
            response.Data.Status.Should().Be("inactive");
            // gender nao foi enviado no corpo, a API deve preservar o valor original
            response.Data.Gender.Should().Be("male");
        }

        [Test]
        public void NaoDeveAlterarUsuario()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("name", "Ronald McDonald Jr");
            jObjectBody.Add("email", UserFactory.UniqueEmail("Ronald McDonald"));
            jObjectBody.Add("status", "active");

            var response = ApiClient<User>.Request(EndpointProvider.User(-user.Id), Method.PUT, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
