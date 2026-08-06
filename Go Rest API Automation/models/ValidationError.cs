using Newtonsoft.Json;

namespace Go_Rest_API_Automation.models
{
    public class ValidationError
    {
        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
