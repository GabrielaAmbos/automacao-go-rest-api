using System.Collections.Generic;
using System.Net;

namespace Go_Rest_API_Automation.client
{
    public class ApiResponse<T>
    {
        private readonly IDictionary<string, string> headers;

        public HttpStatusCode StatusCode { get; }

        public T Data { get; }

        public ApiResponse(HttpStatusCode statusCode, T data, IDictionary<string, string> headers)
        {
            StatusCode = statusCode;
            Data = data;
            this.headers = headers;
        }

        public string Header(string name)
        {
            string value;

            return headers.TryGetValue(name, out value) ? value : null;
        }

        public int IntHeader(string name)
        {
            return int.Parse(Header(name));
        }
    }
}
