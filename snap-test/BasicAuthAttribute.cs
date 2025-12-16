using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text;

namespace snap_test
{
    public class BasicAuthAttribute : Attribute, IAuthorizationFilter
    {
        private const string UserName = "Admin";
        private const string PASSWORD = "Admin@1234";
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            try
            {
                var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                var decodedBytes = Convert.FromBase64String(encodedCredentials);
                var decodedCredentials = Encoding.UTF8.GetString(decodedBytes);

                var credentials = decodedCredentials.Split(':');
                var username = credentials[0];
                var password = credentials[1];

                if (username != UserName || password != PASSWORD)
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            catch
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
