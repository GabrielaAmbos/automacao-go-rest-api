using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;

namespace Go_Rest_API_Automation.tests.comments
{
    [TestFixture]
    public class GetListarComentarios
    {
        private User user;
        private Post post;
        private Comment comment;

        [OneTimeSetUp]
        public void CriarComentario()
        {
            user = UserFactory.Create("Isaac Asimov");
            post = PostFactory.Create(user.Id);
            comment = CommentFactory.Create(post.Id);
        }

        [OneTimeTearDown]
        public void ExcluirComentario()
        {
            CommentFactory.Delete(comment.Id);
            PostFactory.Delete(post.Id);
            UserFactory.Delete(user.Id);
        }

        [Test]
        public void DeveListarComentarios()
        {
            var response = ApiClient<List<Comment>>.Request(EndpointProvider.Comments());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeEmpty();
            response.IntHeader(PaginationHeader.Total).Should().BePositive();
        }

        [Test]
        public void DeveRespeitarOTamanhoDaPagina()
        {
            var response = ApiClient<List<Comment>>.Request(
                EndpointProvider.Comments(), queryParameters: Query.Page(1, 5));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().HaveCount(5);
            response.IntHeader(PaginationHeader.Limit).Should().Be(5);
        }

        [Test]
        public void DeveListarComentariosDoPost()
        {
            var response = ApiClient<List<Comment>>.Request(EndpointProvider.PostComments(post.Id));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().ContainSingle();
            response.Data[0].Id.Should().Be(comment.Id);
            response.Data[0].PostId.Should().Be(post.Id);
        }
    }
}
