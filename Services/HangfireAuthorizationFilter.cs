using Hangfire.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace TINH_FINAL_2256.Services
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            if (httpContext == null) return false;

            // Allow in development
            var env = httpContext.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            if (env != null && env.IsDevelopment()) return true;

            // Only allow authenticated Admins
            var user = httpContext.User;
            return user?.Identity != null && user.Identity.IsAuthenticated && user.IsInRole("Admin");
        }
    }
}
