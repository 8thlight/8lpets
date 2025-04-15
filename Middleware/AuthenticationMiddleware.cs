using _8lpets.Services;

namespace _8lpets.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserService userService)
        {
            // Check if user is authenticated
            var userId = context.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                // Get user from database
                var user = await userService.GetUserByIdAsync(userId.Value);
                if (user != null)
                {
                    // Store user in HttpContext.Items for access in controllers/pages
                    context.Items["User"] = user;
                }
                else
                {
                    // User not found, clear session
                    context.Session.Remove("UserId");
                }
            }

            await _next(context);
        }
    }

    // Extension method to add the middleware to the HTTP request pipeline
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}
