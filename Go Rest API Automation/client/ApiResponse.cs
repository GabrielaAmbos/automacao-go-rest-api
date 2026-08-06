using System.Net;

namespace Go_Rest_API_Automation.client
{
    public class ApiResponse<T>
    {
        public HttpStatusCode StatusCode { get; }

        public T Data { get; }

        public ApiResponse(HttpStatusCode statusCode, T data)
        {
            StatusCode = statusCode;
            Data = data;
        }
    }
}
