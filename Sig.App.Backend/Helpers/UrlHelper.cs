namespace Sig.App.Backend.Helpers
{
    public static class UrlHelper
    {
        public static string ConfirmEmail(string to, string token)
        {
            return $"confirm-email?email={System.Net.WebUtility.UrlEncode(to)}&token={System.Net.WebUtility.UrlEncode(token)}";
        }

        public static string ConfirmChangeEmail(string to, string token)
        {
            return $"confirm-change-email?email={System.Net.WebUtility.UrlEncode(to)}&token={System.Net.WebUtility.UrlEncode(token)}";
        }

        public static string RegistrationAdmin(string to, string token)
        {
            return $"registration/admin?email={System.Net.WebUtility.UrlEncode(to)}&token={System.Net.WebUtility.UrlEncode(token)}";
        }

        public static string RegistrationProjectManager(string to, string token)
        {
            return $"registration/project-manager?email={System.Net.WebUtility.UrlEncode(to)}&token={System.Net.WebUtility.UrlEncode(token)}";
        }

        public static string RegistrationMarketManager(string to, string token)
        {
            return $"registration/merchant?email={System.Net.WebUtility.UrlEncode(to)}&token={System.Net.WebUtility.UrlEncode(token)}";
        }

        public static string RegistrationOrganizationManager(string to, string token)
        {
            return $"registration/organization-manager?email={System.Net.WebUtility.UrlEncode(to)}&token={System.Net.WebUtility.UrlEncode(token)}";
        }

        public static string ResetPassword(string to, string token)
        {
            return $"reset-password?email={System.Net.WebUtility.UrlEncode(to)}&token={System.Net.WebUtility.UrlEncode(token)}";
        }
    }
}
