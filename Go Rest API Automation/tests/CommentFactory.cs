using Go_Rest_API_Automation.client;
using Go_Rest_API_Automation.models;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Go_Rest_API_Automation.tests
{
    public static class CommentFactory
    {
        public static Comment Create(int postId)
        {
            return Create(postId, "Ada Lovelace", "Lorem ipsum comment");
        }

        public static Comment Create(int postId, string name, string body)
        {
            JObject jObjectBody = Body(name, UserFactory.UniqueEmail(name), body);

            return ApiClient<Comment>.Request(EndpointProvider.PostComments(postId), Method.POST, jObjectBody).Data;
        }

        public static JObject Body(string name, string email, string body)
        {
            JObject jObjectBody = new JObject();
            jObjectBody.Add("name", name);
            jObjectBody.Add("email", email);
            jObjectBody.Add("body", body);

            return jObjectBody;
        }

        public static void Delete(int id)
        {
            ApiClient<Comment>.Request(EndpointProvider.Comment(id), Method.DELETE);
        }
    }
}
