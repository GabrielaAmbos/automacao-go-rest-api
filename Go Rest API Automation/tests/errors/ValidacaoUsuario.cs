using System.Collections.Generic;
using System.Linq;
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
    public class ValidacaoUsuario
    {
        private ApiResponse<List<ValidationError>> CriarUsuario(JObject jObjectBody)
        {
            return ApiClient<List<ValidationError>>.Request(
                EndpointProvider.Users(), Method.POST, jObjectBody);
        }

        [Test]
        public void NaoDeveCriarUsuarioSemCamposObrigatorios()
        {
            var response = CriarUsuario(new JObject());

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().HaveCount(4);
            response.Data.Select(error => error.Field)
                .Should().BeEquivalentTo(new[] { "email", "name", "gender", "status" });
        }

        [Test]
        public void NaoDeveCriarUsuarioComEmailDuplicado()
        {
            User existingUser = Hooks.SharedUser;
            JObject jObjectBody = UserFactory.Body("Clone", existingUser.Email, "male", "active");

            var response = CriarUsuario(jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().ContainSingle();
            response.Data[0].Field.Should().Be("email");
            response.Data[0].Message.Should().Be("has already been taken");
        }

        [Test]
        public void NaoDeveCriarUsuarioComEmailInvalido()
        {
            JObject jObjectBody = UserFactory.Body("Email Invalido", "nao-eh-email", "male", "active");

            var response = CriarUsuario(jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Contain(error => error.Field == "email" && error.Message == "is invalid");
        }

        [Test]
        public void NaoDeveCriarUsuarioComGeneroInvalido()
        {
            JObject jObjectBody = UserFactory.Body(
                "Genero Invalido", UserFactory.UniqueEmail("Genero Invalido"), "outro", "active");

            var response = CriarUsuario(jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Contain(error => error.Field == "gender");
        }

        [Test]
        public void NaoDeveCriarUsuarioComStatusInvalido()
        {
            JObject jObjectBody = UserFactory.Body(
                "Status Invalido", UserFactory.UniqueEmail("Status Invalido"), "male", "quase-ativo");

            var response = CriarUsuario(jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Contain(error => error.Field == "status");
        }
    }
}
