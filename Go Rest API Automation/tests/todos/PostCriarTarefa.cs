using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests.todos
{
    [TestFixture]
    public class PostCriarTarefa
    {
        private User user;
        private int createdTodoId;

        [OneTimeSetUp]
        public void CriarUsuario()
        {
            user = UserFactory.Create("Katherine Johnson");
        }

        [OneTimeTearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(user.Id);
        }

        [TearDown]
        public void ExcluirTarefaCriada()
        {
            if (createdTodoId != 0)
            {
                TodoFactory.Delete(createdTodoId);
                createdTodoId = 0;
            }
        }

        [Test]
        public void DeveCriarTarefaParaUsuario()
        {
            JObject jObjectBody = TodoFactory.Body("Calcular trajetoria", TodoFactory.DueOn, "pending");

            var response = ApiClient<Todo>.Request(
                EndpointProvider.UserTodos(user.Id), Method.POST, jObjectBody);

            createdTodoId = response.Data.Id;

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Data.Id.Should().BePositive();
            response.Data.UserId.Should().Be(user.Id);
            response.Data.Title.Should().Be("Calcular trajetoria");
            response.Data.Status.Should().Be("pending");
            response.Data.DueOn.Should().Be(TodoFactory.DueOn);
        }

        [Test]
        public void NaoDeveCriarTarefaComStatusInvalido()
        {
            JObject jObjectBody = TodoFactory.Body("Calcular trajetoria", TodoFactory.DueOn, "quase-la");

            var response = ApiClient<List<ValidationError>>.Request(
                EndpointProvider.UserTodos(user.Id), Method.POST, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Contain(error => error.Field == "status");
        }

        [Test]
        public void NaoDeveCriarTarefaSemCamposObrigatorios()
        {
            var response = ApiClient<List<ValidationError>>.Request(
                EndpointProvider.UserTodos(user.Id), Method.POST, new JObject());

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Contain(error => error.Field == "title" && error.Message == "can't be blank");
        }
    }
}
