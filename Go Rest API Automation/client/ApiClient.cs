using System;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Go_Rest_API_Automation.client
{
    public static class ApiClient<T>
    {
        public static ApiResponse<T> Request(string endpoint, Method method = Method.GET, JObject jObjectBody = null)
        {
            RestClient restClient = new RestClient(UrlProvider.BaseUrl());
            RestRequest restRequest = new RestRequest(endpoint, method);

            restRequest.AddHeader("Authorization", TokenProvider.BasicToken());

            if (jObjectBody != null)
            {
                restRequest.AddParameter("application/json", jObjectBody.ToString(), ParameterType.RequestBody);
            }

            IRestResponse restResponse = restClient.Execute(restRequest);

            return new ApiResponse<T>(restResponse.StatusCode, Deserialize(restResponse));
        }

        private static T Deserialize(IRestResponse restResponse)
        {
            if (string.IsNullOrWhiteSpace(restResponse.Content))
            {
                return default(T);
            }

            if (restResponse.ContentType == null || !restResponse.ContentType.Contains("json"))
            {
                throw new InvalidOperationException(
                    "Expected a JSON response but got " + restResponse.StatusCode +
                    " (" + restResponse.ContentType + ") from " + restResponse.ResponseUri);
            }

            return JsonConvert.DeserializeObject<T>(restResponse.Content);
        }
    }
}
