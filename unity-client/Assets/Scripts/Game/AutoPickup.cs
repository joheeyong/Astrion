using UnityEngine;

namespace Astrion.Game
{
    /// Player-controlled toggle for the on-touch drop pickup. Persisted in
    /// PlayerPrefs so the choice survives restarts.
    public static class AutoPickup
    {
        private const string Key = "autoPickup";

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(Key, 1) == 1; // ON by default
            set
            {
                PlayerPrefs.SetInt(Key, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
