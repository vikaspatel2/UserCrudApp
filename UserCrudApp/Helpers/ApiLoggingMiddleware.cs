using UserCrudApp.Data;
using UserCrudApp.Models;

namespace UserCrudApp.Helpers
{
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ApplicationDbContext db)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var log = new ApiLog
                {
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    User = context.User.Identity?.Name ?? "Anonymous",
                    TimeStamp = DateTime.UtcNow
                };
                db.ApiLog.Add(log);
                await db.SaveChangesAsync();
            }

            await _next(context);
        }
    }
}
