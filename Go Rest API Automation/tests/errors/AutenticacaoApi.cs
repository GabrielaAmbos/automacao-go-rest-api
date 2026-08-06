using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests.errors
{
    [TestFixture]
    public class AutenticacaoApi
    {
        private User user;
        private Post post;

        [OneTimeSetUp]
        public void CriarPost()
        {
            user = UserFactory.Create("Auth Probe");
            post = PostFactory.Create(user.Id);
        }

        [OneTimeTearDown]
        public void ExcluirPost()
        {
            PostFactory.Delete(post.Id);
            UserFactory.Delete(user.Id);
        }

        private JObject NovoUsuario()
        {
            return UserFactory.Body("Sem Token", UserFactory.UniqueEmail("Sem Token"), "male", "active");
        }

        [Test]
        public void NaoDeveCriarUsuarioSemToken()
        {
            var response = ApiClient<ErrorMessage>.RequestWithoutToken(
                EndpointProvider.Users(), Method.POST, NovoUsuario());

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.Data.Message.Should().Be("Authentication failed");
        }

        [Test]
        public void NaoDeveCriarUsuarioComTokenInvalido()
        {
            var response = ApiClient<ErrorMessage>.RequestWithToken(
                EndpointProvider.Users(), "Bearer token-invalido", Method.POST, NovoUsuario());

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.Data.Message.Should().Be("Invalid token");
        }

        [Test]
        public void DeveListarUsuariosSemToken()
        {
            var response = ApiClient<List<User>>.RequestWithoutToken(EndpointProvider.Users());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeEmpty();
        }

        // recurso criado pelo token fica restrito a ele: sem token a API devolve 404 em vez de 401,
        // para nao vazar a existencia do registro
        [Test]
        public void NaoDeveBuscarUsuarioSemToken()
        {
            var response = ApiClient<ErrorMessage>.RequestWithoutToken(EndpointProvider.User(user.Id));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }

        [Test]
        public void NaoDeveAlterarUsuarioSemToken()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("status", "inactive");

            var response = ApiClient<ErrorMessage>.RequestWithoutToken(
                EndpointProvider.User(user.Id), Method.PUT, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");

            var afterAttempt = ApiClient<User>.Request(EndpointProvider.User(user.Id));

            afterAttempt.StatusCode.Should().Be(HttpStatusCode.OK);
            afterAttempt.Data.Status.Should().Be("active");
        }

        [Test]
        public void NaoDeveExcluirUsuarioSemToken()
        {
            var response = ApiClient<ErrorMessage>.RequestWithoutToken(
                EndpointProvider.User(user.Id), Method.DELETE);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");

            var afterAttempt = ApiClient<User>.Request(EndpointProvider.User(user.Id));

            afterAttempt.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public void NaoDeveBuscarPostCriadoPeloTokenSemAutenticacao()
        {
            var response = ApiClient<ErrorMessage>.RequestWithoutToken(EndpointProvider.Post(post.Id));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }

        // ja o conteudo publico de seed continua legivel sem token
        [Test]
        public void DeveBuscarPostPublicoSemToken()
        {
            var posts = ApiClient<List<Post>>.RequestWithoutToken(EndpointProvider.Posts());
            int publicPostId = posts.Data[0].Id;

            var response = ApiClient<Post>.RequestWithoutToken(EndpointProvider.Post(publicPostId));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Id.Should().Be(publicPostId);
        }
    }
}
