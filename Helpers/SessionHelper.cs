using INSolPOS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace INSolPOS.Helpers
{
    public static class SessionHelper
    {
        public const string UserIdKey = "UserId";
        public const string UserNameKey = "UserName";
        public const string UserRoleKey = "UserRole";
        public const string FullNameKey = "FullName";

        public static void SetUser(ISession session, ApplicationUser user)
        {
            session.SetInt32(UserIdKey, user.Id);
            session.SetString(UserNameKey, user.Username);
            session.SetInt32(UserRoleKey, (int)user.Role);
            session.SetString(FullNameKey, user.FullName);
        }

        public static int? GetUserId(ISession session) => session.GetInt32(UserIdKey);
        public static string? GetUserName(ISession session) => session.GetString(UserNameKey);
        public static string? GetFullName(ISession session) => session.GetString(FullNameKey);
        public static UserRole? GetUserRole(ISession session)
        {
            var role = session.GetInt32(UserRoleKey);
            return role.HasValue ? (UserRole)role.Value : null;
        }

        public static bool IsLoggedIn(ISession session) => session.GetInt32(UserIdKey).HasValue;

        public static void ClearSession(ISession session) => session.Clear();
    }

    public abstract class BaseController : Controller
    {
        protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionHelper.UserIdKey);
        protected UserRole? CurrentUserRole => SessionHelper.GetUserRole(HttpContext.Session);
        protected string? CurrentUserName => SessionHelper.GetUserName(HttpContext.Session);
        protected string? CurrentFullName => SessionHelper.GetFullName(HttpContext.Session);

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Skip auth for login pages
            var skipAuth = context.ActionDescriptor.EndpointMetadata
                .Any(m => m is AllowAnonymousAttribute);
            if (skipAuth) return;

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            {
                // Return JSON 401 for AJAX requests instead of redirect
                var isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json")
                          || context.HttpContext.Request.Path.StartsWithSegments("/Products/Search")
                          || context.HttpContext.Request.Path.StartsWithSegments("/Products/GetByBarcode");
                if (isAjax)
                {
                    context.Result = new Microsoft.AspNetCore.Mvc.JsonResult(new { error = "Session expired. Please login again." })
                    {
                        StatusCode = 401
                    };
                }
                else
                {
                    context.Result = RedirectToAction("Login", "Account");
                }
                return;
            }

            ViewBag.CurrentUserId = CurrentUserId;
            ViewBag.CurrentUserRole = CurrentUserRole;
            ViewBag.CurrentUserName = CurrentFullName;
            ViewBag.CurrentRoleName = CurrentUserRole?.ToString();
        }

        protected bool HasRole(params UserRole[] roles) =>
            CurrentUserRole.HasValue && roles.Contains(CurrentUserRole.Value);

        protected bool IsSuperAdmin() => CurrentUserRole == UserRole.SuperAdmin;
        protected bool IsAdmin() => CurrentUserRole is UserRole.SuperAdmin or UserRole.Admin;

        public class AllowAnonymousAttribute : Attribute { }
    }
}
