using Microsoft.AspNetCore.Http;

namespace frontend.Helpers
{
    /// <summary>
    /// Centralized session key constants and helper methods for auth state management.
    /// Ensures mutual exclusion between user and admin sessions.
    /// </summary>
    public static class SessionKeys
    {
        public const string UserId    = "UserId";
        public const string UserName  = "UserName";
        public const string UserEmail = "UserEmail";
        public const string IsAdmin   = "IsAdmin";

        /// <summary>Clears all user session keys.</summary>
        public static void ClearUserSession(this ISession session)
        {
            session.Remove(UserId);
            session.Remove(UserName);
            session.Remove(UserEmail);
        }

        /// <summary>Clears the admin session key.</summary>
        public static void ClearAdminSession(this ISession session)
        {
            session.Remove(IsAdmin);
        }

        /// <summary>Clears all auth session keys (user + admin).</summary>
        public static void LogoutAll(this ISession session)
        {
            session.ClearUserSession();
            session.ClearAdminSession();
        }

        /// <summary>Sets user session and clears admin session (mutual exclusion).</summary>
        public static void LoginUser(this ISession session, string userId, string userName, string userEmail)
        {
            session.ClearAdminSession();
            session.SetString(UserId, userId);
            session.SetString(UserName, userName);
            session.SetString(UserEmail, userEmail);
        }

        /// <summary>Sets admin session and clears user session (mutual exclusion).</summary>
        public static void LoginAdmin(this ISession session)
        {
            session.ClearUserSession();
            session.SetString(IsAdmin, "true");
        }
    }
}
