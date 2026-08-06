using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;

namespace Go_Rest_API_Automation.tests.posts
{
    [TestFixture]
    public class GetListarPosts
    {
        private User user;
        private Post post;

        [OneTimeSetUp]
        public void CriarPost()
        {
            user = UserFactory.Create("Clarice Lispector");
            post = PostFactory.Create(user.Id);
        }

        [OneTimeTearDown]
        public void ExcluirPost()
        {
            PostFactory.Delete(post.Id);
            UserFactory.Delete(user.Id);
        }

        [Test]
        public void DeveListarPosts()
        {
            var response = ApiClient<List<Post>>.Request(EndpointProvider.Posts());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeEmpty();
            response.IntHeader(PaginationHeader.Total).Should().BePositive();
        }

        [Test]
        public void DeveRespeitarOTamanhoDaPagina()
        {
            var response = ApiClient<List<Post>>.Request(
                EndpointProvider.Posts(), queryParameters: Query.Page(1, 5));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().HaveCount(5);
            response.IntHeader(PaginationHeader.Limit).Should().Be(5);
        }

        [Test]
        public void DeveListarPostsDoUsuario()
        {
            var response = ApiClient<List<Post>>.Request(EndpointProvider.UserPosts(user.Id));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().ContainSingle();
            response.Data[0].Id.Should().Be(post.Id);
            response.Data[0].UserId.Should().Be(user.Id);
        }

        [Test]
        public void DeveRetornarListaVaziaParaUsuarioSemPosts()
        {
            User userWithoutPosts = UserFactory.Create("Machado de Assis");

            var response = ApiClient<List<Post>>.Request(EndpointProvider.UserPosts(userWithoutPosts.Id));

            UserFactory.Delete(userWithoutPosts.Id);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().BeEmpty();
        }
    }
}
