using Microsoft.AspNetCore.WebUtilities;

namespace snap_test.Helpers
{
    /// <summary>
    /// Builds the standard TestingAPIs error body: { status, error, message }.
    /// </summary>
    public static class ApiResponse
    {
        public static object Error(int status, string message) => new
        {
            status,
            error = ReasonPhrases.GetReasonPhrase(status),
            message
        };
    }
}
