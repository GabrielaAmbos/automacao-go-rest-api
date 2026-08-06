using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;

namespace Go_Rest_API_Automation.tests.posts
{
    [TestFixture]
    public class GetBuscarPost
    {
        private User user;
        private Post post;

        [OneTimeSetUp]
        public void CriarPost()
        {
            user = UserFactory.Create("Ernest Hemingway");
            post = PostFactory.Create(user.Id, "O velho e o mar", "Era um velho que pescava sozinho.");
        }

        [OneTimeTearDown]
        public void ExcluirPost()
        {
            PostFactory.Delete(post.Id);
            UserFactory.Delete(user.Id);
        }

        [Test]
        public void DeveBuscarPost()
        {
            var response = ApiClient<Post>.Request(EndpointProvider.Post(post.Id));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Id.Should().Be(post.Id);
            response.Data.UserId.Should().Be(user.Id);
            response.Data.Title.Should().Be("O velho e o mar");
            response.Data.Body.Should().Be("Era um velho que pescava sozinho.");
        }

        [Test]
        public void NaoDeveBuscarPost()
        {
            var response = ApiClient<ErrorMessage>.Request(EndpointProvider.Post(-post.Id));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
