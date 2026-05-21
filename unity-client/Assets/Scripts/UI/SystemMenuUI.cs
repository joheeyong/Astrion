using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astrion.UI
{
    public class SystemMenuUI : MonoBehaviour
    {
        public static SystemMenuUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private Button charSelectButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button closeButton;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (panel) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (charSelectButton != null) charSelectButton.onClick.AddListener(OnCharSelect);
            if (logoutButton    != null) logoutButton.onClick.AddListener(OnLogout);
            if (quitButton      != null) quitButton.onClick.AddListener(OnQuit);
            if (closeButton     != null) closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (IsTextInputFocused()) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            if (IsOpen) { Close(); return; }
            if (AnyOtherModalOpen()) return;
            Open();
        }

        private bool IsTextInputFocused()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            return sel != null && sel.GetComponent<InputField>() != null;
        }

        private bool AnyOtherModalOpen()
        {
            var dlg = DialogueUI.Instance;
            if (dlg != null && dlg.IsOpen) return true;

            var inv = Object.FindObjectOfType<InventoryUI>();
            if (inv != null && inv.IsOpen()) return true;

            var ci = Object.FindObjectOfType<CharacterInfoUI>();
            if (ci != null && ci.IsOpen()) return true;

            var sw = Object.FindObjectOfType<SkillWindowUI>();
            if (sw != null && sw.IsOpen()) return true;

            var shop = ShopUI.Instance;
            if (shop != null && shop.IsOpen) return true;

            var ql = QuestLogUI.Instance;
            if (ql != null && ql.IsOpen) return true;

            return false;
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }
        public void Open() { if (panel) panel.SetActive(true); }
        public void Close() { if (panel) panel.SetActive(false); }

        private void OnCharSelect()
        {
            Close();
            // PlayerStateManager keeps the player's data; selecting another character will overwrite it.
            SceneManager.LoadScene("CharacterSelectScene");
        }

        /// Full logout: drop cached credentials so ReconnectSystem doesn't
        /// silently re-LOGIN, tear down the TCP connection, and return to the
        /// login screen. Different from 'Character Select' which keeps the
        /// session alive and just swaps the active character.
        private void OnLogout()
        {
            Close();
            // Block the auto-reconnect path before we tear the socket — the
            // ReconnectSystem otherwise sees the disconnect and immediately
            // tries to log back in as the same user.
            Astrion.Network.SessionCredentials.Username = "";
            Astrion.Network.SessionCredentials.Password = "";

            if (Astrion.Network.NetworkManager.Instance != null)
                Astrion.Network.NetworkManager.Instance.Disconnect();

            // playerId is per-session; username stays in PlayerPrefs so the
            // login field repopulates next launch (no need to retype).
            PlayerPrefs.DeleteKey("playerId");
            PlayerPrefs.Save();

            SceneManager.LoadScene("LoginScene");
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
