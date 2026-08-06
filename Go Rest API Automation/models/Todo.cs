using Newtonsoft.Json;

namespace Go_Rest_API_Automation.models
{
    public class Todo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("user_id")]
        public int UserId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("due_on")]
        public string DueOn { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
