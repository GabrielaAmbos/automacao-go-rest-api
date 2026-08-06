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
    public class PostCriarUmUsuario
    {
        private int createdUserId;

        [Test]
        public void DeveCriarUmUsuario()
        {
            string email = UserFactory.UniqueEmail("Tomas Oliver");

            JObject jObjectBody = new JObject();
            jObjectBody.Add("name", "Tomas Oliver");
            jObjectBody.Add("gender", "male");
            jObjectBody.Add("email", email);
            jObjectBody.Add("status", "active");

            var response = ApiClient<User>.Request(EndpointProvider.Users(), Method.POST, jObjectBody);

            createdUserId = response.Data.Id;

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Data.Id.Should().BePositive();
            response.Data.Name.Should().Be("Tomas Oliver");
            response.Data.Gender.Should().Be("male");
            response.Data.Email.Should().Be(email);
            response.Data.Status.Should().Be("active");
        }

        [TearDown]
        public void ExcluirUsuarioCriado()
        {
            if (createdUserId != 0)
            {
                UserFactory.Delete(createdUserId);
                createdUserId = 0;
            }
        }
    }
}
