using System;
using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Go_Rest_API_Automation.tests
{
    public static class UserFactory
    {
        public static User Create(string name)
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("name", name);
            jObjectBody.Add("gender", "male");
            jObjectBody.Add("email", UniqueEmail(name));
            jObjectBody.Add("status", "active");

            return ApiClient<User>.Request(EndpointProvider.Users(), Method.POST, jObjectBody).Data;
        }

        public static void Delete(int id)
        {
            ApiClient<User>.Request(EndpointProvider.User(id), Method.DELETE);
        }

        // a API rejeita e-mail duplicado, entao cada execucao precisa de um endereco novo
        public static string UniqueEmail(string name)
        {
            return name.Replace(" ", ".").ToLower() + "." + Guid.NewGuid().ToString("N") + "@example.test";
        }
    }
}
