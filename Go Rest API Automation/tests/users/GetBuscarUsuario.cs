using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;

namespace Go_Rest_API_Automation.tests.users
{
    [TestFixture]
    public class GetBuscarUsuario
    {
        [Test]
        public void DeveBuscarUsuario()
        {
            User sharedUser = Hooks.SharedUser;

            var response = ApiClient<User>.Request(EndpointProvider.User(sharedUser.Id));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Id.Should().Be(sharedUser.Id);
            response.Data.Name.Should().Be("Leroy Borgan");
            response.Data.Email.Should().Be(sharedUser.Email);
            response.Data.Gender.Should().Be("male");
            response.Data.Status.Should().Be("active");
        }

        [Test]
        public void NaoDeveBuscarUsuario()
        {
            var response = ApiClient<ErrorMessage>.Request(EndpointProvider.User(-Hooks.SharedUser.Id));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
