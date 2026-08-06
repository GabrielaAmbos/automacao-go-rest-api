using System.Collections.Generic;

namespace Go_Rest_API_Automation.utils
{
    public static class Query
    {
        public static IDictionary<string, string> Page(int page, int perPage)
        {
            return new Dictionary<string, string>
            {
                { "page", page.ToString() },
                { "per_page", perPage.ToString() }
            };
        }

        public static IDictionary<string, string> Filter(string field, string value)
        {
            return new Dictionary<string, string> { { field, value } };
        }
    }
}
