using System.Security.Claims;
using BibekSchool.Services;

namespace BibekSchool.Middlewares
{
    public class PopulateCommonDataMiddleware
    {
        private readonly RequestDelegate _next;

        public PopulateCommonDataMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Get user's highest priority role (MainAdmin > Admin > Teacher > Student)
                        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                        var priorityRoles = new[] { "MainAdmin", "Admin", "Teacher", "Student" };
                        var role = priorityRoles.FirstOrDefault(r => roles.Contains(r));

                        // Resolve INotificationService from request services
                        var notificationService = context.RequestServices.GetService<INotificationService>();
                        if (notificationService != null)
                        {
                            var count = await notificationService.GetUnreadCountAsync(userId, role);
                            context.Items["UnreadNotificationsCount"] = count;
                        }
                    }
                }
            }
            catch
            {
                // Swallow any errors here; middleware should not break the request pipeline.
            }

            await _next(context);
        }
    }
}
