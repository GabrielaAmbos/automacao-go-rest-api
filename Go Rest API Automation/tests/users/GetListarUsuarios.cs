using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils;
using Go_Rest_API_Automation.utils.providers;
using NUnit.Framework;

namespace Go_Rest_API_Automation.tests.users
{
    [TestFixture]
    public class GetListarUsuarios
    {
        [Test]
        public void DeveListarUsuarios()
        {
            var response = ApiClient<List<User>>.Request(EndpointProvider.Users());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeEmpty();
            response.IntHeader(PaginationHeader.Total).Should().BePositive();
            response.IntHeader(PaginationHeader.Pages).Should().BePositive();
        }

        [Test]
        public void DeveRespeitarOTamanhoDaPagina()
        {
            var response = ApiClient<List<User>>.Request(
                EndpointProvider.Users(), queryParameters: Query.Page(1, 5));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().HaveCount(5);
            response.IntHeader(PaginationHeader.Limit).Should().Be(5);
            response.IntHeader(PaginationHeader.Page).Should().Be(1);
        }

        [Test]
        public void DeveNavegarEntrePaginas()
        {
            var firstPage = ApiClient<List<User>>.Request(
                EndpointProvider.Users(), queryParameters: Query.Page(1, 5));

            var secondPage = ApiClient<List<User>>.Request(
                EndpointProvider.Users(), queryParameters: Query.Page(2, 5));

            secondPage.StatusCode.Should().Be(HttpStatusCode.OK);
            secondPage.IntHeader(PaginationHeader.Page).Should().Be(2);
            secondPage.Data.Select(user => user.Id)
                .Should().NotIntersectWith(firstPage.Data.Select(user => user.Id));
        }

        [Test]
        public void DeveFiltrarPorStatus()
        {
            var response = ApiClient<List<User>>.Request(
                EndpointProvider.Users(), queryParameters: Query.Filter("status", "inactive"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeEmpty();
            response.Data.Should().OnlyContain(user => user.Status == "inactive");
        }

        [Test]
        public void DeveFiltrarPorGenero()
        {
            var response = ApiClient<List<User>>.Request(
                EndpointProvider.Users(), queryParameters: Query.Filter("gender", "female"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeEmpty();
            response.Data.Should().OnlyContain(user => user.Gender == "female");
        }

        [Test]
        public void DeveFiltrarPorEmail()
        {
            User sharedUser = Hooks.SharedUser;

            var response = ApiClient<List<User>>.Request(
                EndpointProvider.Users(), queryParameters: Query.Filter("email", sharedUser.Email));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().ContainSingle();
            response.Data[0].Id.Should().Be(sharedUser.Id);
        }
    }
}
