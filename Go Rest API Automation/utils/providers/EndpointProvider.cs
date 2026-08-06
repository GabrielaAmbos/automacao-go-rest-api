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

        public static string Posts()
        {
            return "/posts";
        }

        public static string Post(int id)
        {
            return "/posts/" + id;
        }

        public static string UserPosts(int userId)
        {
            return User(userId) + "/posts";
        }

        public static string Comments()
        {
            return "/comments";
        }

        public static string Comment(int id)
        {
            return "/comments/" + id;
        }

        public static string PostComments(int postId)
        {
            return Post(postId) + "/comments";
        }

        public static string Todos()
        {
            return "/todos";
        }

        public static string Todo(int id)
        {
            return "/todos/" + id;
        }

        public static string UserTodos(int userId)
        {
            return User(userId) + "/todos";
        }
    }
}
