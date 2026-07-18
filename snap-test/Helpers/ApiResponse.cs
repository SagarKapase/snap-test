using Microsoft.AspNetCore.WebUtilities;

namespace snap_test.Helpers
{
    /// <summary>
    /// Builds the standard APIBee error body: { status, error, message }.
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
