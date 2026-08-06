using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Go_Rest_API_Automation.tests
{
    public static class TodoFactory
    {
        public const string DueOn = "2030-12-31T00:00:00.000+05:30";

        public static Todo Create(int userId)
        {
            return Create(userId, "Lorem ipsum task", "pending");
        }

        public static Todo Create(int userId, string title, string status)
        {
            JObject jObjectBody = Body(title, DueOn, status);

            return ApiClient<Todo>.Request(EndpointProvider.UserTodos(userId), Method.POST, jObjectBody).Data;
        }

        public static JObject Body(string title, string dueOn, string status)
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("title", title);
            jObjectBody.Add("due_on", dueOn);
            jObjectBody.Add("status", status);

            return jObjectBody;
        }

        public static void Delete(int id)
        {
            ApiClient<Todo>.Request(EndpointProvider.Todo(id), Method.DELETE);
        }
    }
}
