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
    public class PutAlterarTarefa
    {
        private User user;
        private Todo todo;

        [OneTimeSetUp]
        public void CriarUsuario()
        {
            user = UserFactory.Create("Barbara Liskov");
        }

        [OneTimeTearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(user.Id);
        }

        [SetUp]
        public void CriarTarefa()
        {
            todo = TodoFactory.Create(user.Id, "Escrever especificacao", "pending");
        }

        [TearDown]
        public void ExcluirTarefa()
        {
            TodoFactory.Delete(todo.Id);
        }

        [Test]
        public void DeveConcluirTarefa()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("status", "completed");

            var response = ApiClient<Todo>.Request(EndpointProvider.Todo(todo.Id), Method.PUT, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Id.Should().Be(todo.Id);
            response.Data.Status.Should().Be("completed");
            // title nao foi enviado no corpo, a API deve preservar o valor original
            response.Data.Title.Should().Be("Escrever especificacao");
        }

        [Test]
        public void NaoDeveAlterarTarefaInexistente()
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("status", "completed");

            var response = ApiClient<ErrorMessage>.Request(
                EndpointProvider.Todo(-todo.Id), Method.PUT, jObjectBody);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
