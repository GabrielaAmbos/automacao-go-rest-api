using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests.users
{
    [TestFixture]
    public class DelExcluirUsuario
    {
        private User user;

        [SetUp]
        public void CriarUsuario()
        {
            user = UserFactory.Create("Susan Sanders");
        }

        [TearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(user.Id);
        }

        [Test]
        public void DeveExcluirUsuarioExistente()
        {
            var response = ApiClient<User>.Request(EndpointProvider.User(user.Id), Method.DELETE);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.Data.Should().BeNull();
        }

        [Test]
        public void NaoDeveExcluirUsuarioInexistente()
        {
            var response = ApiClient<ErrorMessage>.Request(
                EndpointProvider.User(-user.Id), Method.DELETE);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }

        [Test]
        public void NaoDeveBuscarUsuarioDepoisDeExcluir()
        {
            ApiClient<User>.Request(EndpointProvider.User(user.Id), Method.DELETE);

            var response = ApiClient<ErrorMessage>.Request(EndpointProvider.User(user.Id));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
