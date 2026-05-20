using UnityEngine;

namespace Astrion.Network
{
    /// Resolves the game server address from three sources, in priority order:
    /// 1. PlayerPrefs (set via in-game settings or `NetworkConfig.SetServer`)
    /// 2. Environment variable (ASTRION_SERVER_HOST / ASTRION_SERVER_PORT) — useful
    ///    in CI, dev machines, or shipping different builds without recompiling.
    /// 3. Production defaults baked into the binary.
    public static class NetworkConfig
    {
        public const string DefaultHost = "3.38.109.138";
        public const int    DefaultPort = 9000;

        private const string PrefHostKey = "net.serverHost";
        private const string PrefPortKey = "net.serverPort";
        private const string EnvHost = "ASTRION_SERVER_HOST";
        private const string EnvPort = "ASTRION_SERVER_PORT";

        public static string Host
        {
            get
            {
                string pref = PlayerPrefs.GetString(PrefHostKey, "");
                if (!string.IsNullOrEmpty(pref)) return pref;
                try
                {
                    string env = System.Environment.GetEnvironmentVariable(EnvHost);
                    if (!string.IsNullOrEmpty(env)) return env;
                }
                catch { /* sandboxed players may forbid env access */ }
                return DefaultHost;
            }
        }

        public static int Port
        {
            get
            {
                int pref = PlayerPrefs.GetInt(PrefPortKey, 0);
                if (pref > 0) return pref;
                try
                {
                    string env = System.Environment.GetEnvironmentVariable(EnvPort);
                    if (int.TryParse(env, out int p) && p > 0) return p;
                }
                catch { /* ignore */ }
                return DefaultPort;
            }
        }

        public static void SetServer(string host, int port)
        {
            if (!string.IsNullOrEmpty(host)) PlayerPrefs.SetString(PrefHostKey, host);
            else PlayerPrefs.DeleteKey(PrefHostKey);
            if (port > 0) PlayerPrefs.SetInt(PrefPortKey, port);
            else PlayerPrefs.DeleteKey(PrefPortKey);
            PlayerPrefs.Save();
        }

        public static void ResetToDefault()
        {
            PlayerPrefs.DeleteKey(PrefHostKey);
            PlayerPrefs.DeleteKey(PrefPortKey);
            PlayerPrefs.Save();
        }

        public static string DisplayString => $"{Host}:{Port}";
    }
}
