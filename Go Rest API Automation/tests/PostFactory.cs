using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Go_Rest_API_Automation.tests
{
    public static class PostFactory
    {
        public static Post Create(int userId)
        {
            return Create(userId, "Lorem ipsum title", "Lorem ipsum body");
        }

        public static Post Create(int userId, string title, string body)
        {
            JObject jObjectBody = Body(title, body);

            return ApiClient<Post>.Request(EndpointProvider.UserPosts(userId), Method.POST, jObjectBody).Data;
        }

        public static JObject Body(string title, string body)
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("title", title);
            jObjectBody.Add("body", body);

            return jObjectBody;
        }

        public static void Delete(int id)
        {
            ApiClient<Post>.Request(EndpointProvider.Post(id), Method.DELETE);
        }
    }
}
