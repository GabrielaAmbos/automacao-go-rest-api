using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;

namespace Go_Rest_API_Automation.tests.todos
{
    [TestFixture]
    public class GetListarTarefas
    {
        private User user;
        private Todo todo;

        [OneTimeSetUp]
        public void CriarTarefa()
        {
            user = UserFactory.Create("Margaret Hamilton");
            todo = TodoFactory.Create(user.Id);
        }

        [OneTimeTearDown]
        public void ExcluirTarefa()
        {
            TodoFactory.Delete(todo.Id);
            UserFactory.Delete(user.Id);
        }

        [Test]
        public void DeveListarTarefas()
        {
            var response = ApiClient<List<Todo>>.Request(EndpointProvider.Todos());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeEmpty();
            response.IntHeader(PaginationHeader.Total).Should().BePositive();
        }

        [Test]
        public void DeveRespeitarOTamanhoDaPagina()
        {
            var response = ApiClient<List<Todo>>.Request(
                EndpointProvider.Todos(), queryParameters: Query.Page(1, 5));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().HaveCount(5);
            response.IntHeader(PaginationHeader.Limit).Should().Be(5);
        }

        [Test]
        public void DeveFiltrarPorStatus()
        {
            var response = ApiClient<List<Todo>>.Request(
                EndpointProvider.Todos(), queryParameters: Query.Filter("status", "completed"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeEmpty();
            response.Data.Should().OnlyContain(item => item.Status == "completed");
        }

        [Test]
        public void DeveListarTarefasDoUsuario()
        {
            var response = ApiClient<List<Todo>>.Request(EndpointProvider.UserTodos(user.Id));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().ContainSingle();
            response.Data[0].Id.Should().Be(todo.Id);
            response.Data[0].UserId.Should().Be(user.Id);
        }
    }
}
