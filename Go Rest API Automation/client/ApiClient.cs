using System;
using System.Collections.Generic;
using Go_Rest_API_Automation.utils.providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Go_Rest_API_Automation.client
{
    public static class ApiClient<T>
    {
        public static ApiResponse<T> Request(
            string endpoint,
            Method method = Method.GET,
            JObject jObjectBody = null,
            IDictionary<string, string> queryParameters = null)
        {
            return Send(endpoint, method, jObjectBody, queryParameters, TokenProvider.BasicToken());
        }

        public static ApiResponse<T> RequestWithToken(
            string endpoint,
            string authorization,
            Method method = Method.GET,
            JObject jObjectBody = null)
        {
            return Send(endpoint, method, jObjectBody, null, authorization);
        }

        public static ApiResponse<T> RequestWithoutToken(
            string endpoint,
            Method method = Method.GET,
            JObject jObjectBody = null)
        {
            return Send(endpoint, method, jObjectBody, null, null);
        }

        private static ApiResponse<T> Send(
            string endpoint,
            Method method,
            JObject jObjectBody,
            IDictionary<string, string> queryParameters,
            string authorization)
        {
            RestClient restClient = new RestClient(UrlProvider.BaseUrl());
            RestRequest restRequest = new RestRequest(endpoint, method);

            if (authorization != null)
            {
                restRequest.AddHeader("Authorization", authorization);
            }

            if (queryParameters != null)
            {
                foreach (KeyValuePair<string, string> queryParameter in queryParameters)
                {
                    restRequest.AddQueryParameter(queryParameter.Key, queryParameter.Value);
                }
            }

            if (jObjectBody != null)
            {
                restRequest.AddParameter("application/json", jObjectBody.ToString(), ParameterType.RequestBody);
            }

            IRestResponse restResponse = restClient.Execute(restRequest);

            return new ApiResponse<T>(restResponse.StatusCode, Deserialize(restResponse), ReadHeaders(restResponse));
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

        private static IDictionary<string, string> ReadHeaders(IRestResponse restResponse)
        {
            Dictionary<string, string> headers =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in restResponse.Headers)
            {
                headers[header.Name] = header.Value == null ? null : header.Value.ToString();
            }

            return headers;
        }
    }
}
