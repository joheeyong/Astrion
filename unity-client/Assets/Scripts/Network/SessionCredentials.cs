namespace Astrion.Network
{
    /// In-memory credentials retained after a successful login so the
    /// ReconnectSystem can re-LOGIN automatically. Cleared on process exit
    /// (deliberately not persisted — PlayerPrefs is plain-text and shared).
    public static class SessionCredentials
    {
        public static string Username = "";
        public static string Password = "";
        public static bool HasCredentials => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
    }
}
