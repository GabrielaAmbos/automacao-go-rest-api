using System;

namespace Go_Rest_API_Automation.utils.providers
{
    public static class TokenProvider
    {
        private const string TokenVariable = "GOREST_TOKEN";

        public static string BasicToken()
        {
            string token = Environment.GetEnvironmentVariable(TokenVariable);

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    "Environment variable " + TokenVariable + " is not set. " +
                    "Get a token at https://gorest.co.in and export it before running the tests.");
            }

            return "Bearer " + token;
        }
    }
}
