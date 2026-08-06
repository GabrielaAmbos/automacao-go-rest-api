using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;
using RestSharp;

namespace Go_Rest_API_Automation.tests.todos
{
    [TestFixture]
    public class DelExcluirTarefa
    {
        private User user;
        private Todo todo;

        [OneTimeSetUp]
        public void CriarUsuario()
        {
            user = UserFactory.Create("Radia Perlman");
        }

        [OneTimeTearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(user.Id);
        }

        [SetUp]
        public void CriarTarefa()
        {
            todo = TodoFactory.Create(user.Id);
        }

        [TearDown]
        public void ExcluirTarefa()
        {
            TodoFactory.Delete(todo.Id);
        }

        [Test]
        public void DeveExcluirTarefaExistente()
        {
            var response = ApiClient<Todo>.Request(EndpointProvider.Todo(todo.Id), Method.DELETE);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.Data.Should().BeNull();
        }

        [Test]
        public void NaoDeveExcluirTarefaInexistente()
        {
            var response = ApiClient<ErrorMessage>.Request(
                EndpointProvider.Todo(-todo.Id), Method.DELETE);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
