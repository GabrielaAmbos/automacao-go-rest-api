using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;

namespace Go_Rest_API_Automation.tests.todos
{
    [TestFixture]
    public class GetBuscarTarefa
    {
        private User user;
        private Todo todo;

        [OneTimeSetUp]
        public void CriarTarefa()
        {
            user = UserFactory.Create("Linus Torvalds");
            todo = TodoFactory.Create(user.Id, "Revisar pull request", "pending");
        }

        [OneTimeTearDown]
        public void ExcluirTarefa()
        {
            TodoFactory.Delete(todo.Id);
            UserFactory.Delete(user.Id);
        }

        [Test]
        public void DeveBuscarTarefa()
        {
            var response = ApiClient<Todo>.Request(EndpointProvider.Todo(todo.Id));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Id.Should().Be(todo.Id);
            response.Data.UserId.Should().Be(user.Id);
            response.Data.Title.Should().Be("Revisar pull request");
            response.Data.Status.Should().Be("pending");
            response.Data.DueOn.Should().Be(TodoFactory.DueOn);
        }

        [Test]
        public void NaoDeveBuscarTarefa()
        {
            var response = ApiClient<ErrorMessage>.Request(EndpointProvider.Todo(-todo.Id));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Data.Message.Should().Be("Resource not found");
        }
    }
}
