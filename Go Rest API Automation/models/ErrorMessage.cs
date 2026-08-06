using Newtonsoft.Json;

namespace Go_Rest_API_Automation.models
{
    public class ErrorMessage
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
