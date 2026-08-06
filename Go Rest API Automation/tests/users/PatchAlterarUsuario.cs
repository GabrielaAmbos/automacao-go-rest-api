using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests.users
{
    [TestFixture]
    public class PatchAlterarUsuario
    {
        private User user;

        [SetUp]
        public void CriarUsuario()
        {
            user = UserFactory.Create("Grace Hopper");
        }

        [TearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(user.Id);
        }

        [Test]
        public void DeveAlterarApenasOStatus()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("status", "inactive");

            var response = ApiClient<User>.Request(EndpointProvider.User(user.Id), Method.PATCH, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Status.Should().Be("inactive");
            response.Data.Name.Should().Be(user.Name);
            response.Data.Email.Should().Be(user.Email);
            response.Data.Gender.Should().Be(user.Gender);
        }

        [Test]
        public void NaoDeveAlterarUsuarioInexistente()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("status", "inactive");

            var response = ApiClient<ErrorMessage>.Request(
                EndpointProvider.User(-user.Id), Method.PATCH, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
