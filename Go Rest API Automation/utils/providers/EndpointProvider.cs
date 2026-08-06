namespace Go_Rest_API_Automation.utils.providers
{
    public static class EndpointProvider
    {
        public static string Users()
        {
            return "/users";
        }

        public static string User(int id)
        {
            return "/users/" + id;
        }
    }
}
