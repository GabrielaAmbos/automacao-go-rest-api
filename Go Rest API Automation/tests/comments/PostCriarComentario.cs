using System.Collections.Generic;
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
    public class PostCriarComentario
    {
        private User user;
        private Post post;
        private int createdCommentId;

        [OneTimeSetUp]
        public void CriarPost()
        {
            user = UserFactory.Create("Ursula Le Guin");
            post = PostFactory.Create(user.Id);
        }

        [OneTimeTearDown]
        public void ExcluirPost()
        {
            PostFactory.Delete(post.Id);
            UserFactory.Delete(user.Id);
        }

        [TearDown]
        public void ExcluirComentarioCriado()
        {
            if (createdCommentId != 0)
            {
                CommentFactory.Delete(createdCommentId);
                createdCommentId = 0;
            }
        }

        [Test]
        public void DeveCriarComentarioNoPost()
        {
            string email = UserFactory.UniqueEmail("Alan Turing");
            JObject jObjectBody = CommentFactory.Body("Alan Turing", email, "Analise interessante.");

            var response = ApiClient<Comment>.Request(
                EndpointProvider.PostComments(post.Id), Method.POST, jObjectBody);

            createdCommentId = response.Data.Id;

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Data.Id.Should().BePositive();
            response.Data.PostId.Should().Be(post.Id);
            response.Data.Name.Should().Be("Alan Turing");
            response.Data.Email.Should().Be(email);
            response.Data.Body.Should().Be("Analise interessante.");
        }

        [Test]
        public void NaoDeveCriarComentarioComEmailInvalido()
        {
            JObject jObjectBody = CommentFactory.Body("Alan Turing", "nao-eh-email", "Analise interessante.");

            var response = ApiClient<List<ValidationError>>.Request(
                EndpointProvider.PostComments(post.Id), Method.POST, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Contain(error => error.Field == "email" && error.Message == "is invalid");
        }

        [Test]
        public void NaoDeveCriarComentarioSemCamposObrigatorios()
        {
            var response = ApiClient<List<ValidationError>>.Request(
                EndpointProvider.PostComments(post.Id), Method.POST, new JObject());

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Contain(error => error.Field == "name" && error.Message == "can't be blank");
            response.Data.Should().Contain(error => error.Field == "body" && error.Message == "can't be blank");
        }
    }
}
