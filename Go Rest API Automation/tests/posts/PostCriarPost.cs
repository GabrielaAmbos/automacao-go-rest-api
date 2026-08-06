using System.Collections.Generic;
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
    public class PostCriarPost
    {
        private User user;
        private int createdPostId;

        [OneTimeSetUp]
        public void CriarUsuario()
        {
            user = UserFactory.Create("Jorge Amado");
        }

        [OneTimeTearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(user.Id);
        }

        [TearDown]
        public void ExcluirPostCriado()
        {
            if (createdPostId != 0)
            {
                PostFactory.Delete(createdPostId);
                createdPostId = 0;
            }
        }

        [Test]
        public void DeveCriarPostParaUsuario()
        {
            JObject jObjectBody = PostFactory.Body("Capitaes da Areia", "O cais fervilhava de gente.");

            var response = ApiClient<Post>.Request(
                EndpointProvider.UserPosts(user.Id), Method.POST, jObjectBody);

            createdPostId = response.Data.Id;

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Data.Id.Should().BePositive();
            response.Data.UserId.Should().Be(user.Id);
            response.Data.Title.Should().Be("Capitaes da Areia");
            response.Data.Body.Should().Be("O cais fervilhava de gente.");
        }

        [Test]
        public void NaoDeveCriarPostSemCamposObrigatorios()
        {
            var response = ApiClient<List<ValidationError>>.Request(
                EndpointProvider.UserPosts(user.Id), Method.POST, new JObject());

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Contain(error => error.Field == "title" && error.Message == "can't be blank");
            response.Data.Should().Contain(error => error.Field == "body" && error.Message == "can't be blank");
        }

        [Test]
        public void NaoDeveCriarPostParaUsuarioInexistente()
        {
            JObject jObjectBody = PostFactory.Body("Titulo orfao", "Corpo orfao");

            var response = ApiClient<List<ValidationError>>.Request(
                EndpointProvider.UserPosts(-user.Id), Method.POST, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Contain(error => error.Field == "user");
        }
    }
}
