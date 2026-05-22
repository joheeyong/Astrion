using Astrion.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astrion.UI
{
    public class LoginUI : MonoBehaviour
    {
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Text statusText;

        private bool _waitingForResponse;

        private void Start()
        {
            // Auto-fill saved username
            string saved = PlayerPrefs.GetString("username", "");
            if (!string.IsNullOrEmpty(saved))
                usernameInput.text = saved;

            loginButton.onClick.AddListener(OnLoginClicked);
            registerButton.onClick.AddListener(OnRegisterClicked);

            NetworkManager.Instance.OnPacketReceived += HandlePacket;
        }

        private void OnLoginClicked()
        {
            if (_waitingForResponse) return;

            string username = usernameInput.text.Trim();
            string password = passwordInput.text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetStatus("Please enter username and password.");
                return;
            }

            SetStatus("Connecting...");
            _waitingForResponse = true;

            if (NetworkManager.Instance.IsConnected)
            {
                SendLogin(username, password, false);
            }
            else
            {
                NetworkManager.Instance.OnConnected += () => SendLogin(username, password, false);
                NetworkManager.Instance.Connect();
            }
        }

        private void OnRegisterClicked()
        {
            if (_waitingForResponse) return;

            string username = usernameInput.text.Trim();
            string password = passwordInput.text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetStatus("Please enter username and password.");
                return;
            }

            if (password.Length < 4)
            {
                SetStatus("Password must be at least 4 characters.");
                return;
            }

            SetStatus("Connecting...");
            _waitingForResponse = true;

            if (NetworkManager.Instance.IsConnected)
            {
                SendLogin(username, password, true);
            }
            else
            {
                NetworkManager.Instance.OnConnected += () => SendLogin(username, password, true);
                NetworkManager.Instance.Connect();
            }
        }

        private void SendLogin(string username, string password, bool isRegister)
        {
            SetStatus(isRegister ? "Registering..." : "Logging in...");
            // Hash on the client. Server expects the digest now, not the
            // plaintext. See PasswordHasher for rationale.
            string hashed = Astrion.Network.PasswordHasher.Sha256Hex(password);
            string payload = JsonUtility.ToJson(new LoginRequest
            {
                username = username,
                password = hashed,
                isRegister = isRegister,
                clientVersion = Astrion.Network.Version.Current,
            });
            NetworkManager.Instance.SendPacket(PacketType.Login, payload);
        }

        private void HandlePacket(GamePacket packet)
        {
            if (packet.Type != PacketType.LoginResult) return;

            var result = JsonUtility.FromJson<LoginResponse>(packet.Payload);
            _waitingForResponse = false;

            if (result.success)
            {
                PlayerPrefs.SetString("username", usernameInput.text.Trim());
                PlayerPrefs.SetString("playerId", result.playerId);
                PlayerPrefs.Save();

                // Cache credentials in-memory for auto-reconnect (not persisted).
                // Store the hashed form — the plaintext is never retained,
                // so a memory scrape / coredump can't read the real password.
                Astrion.Network.SessionCredentials.Username = usernameInput.text.Trim();
                Astrion.Network.SessionCredentials.Password =
                    Astrion.Network.PasswordHasher.Sha256Hex(passwordInput.text);

                // Request persisted game state (quest progress, collected items) from server.
                // Server must support STATE_REQUEST (0x09); see PlayerStateManager.ServerSupportsState
                // as the runtime guard.
                PlayerStateManager.Instance?.RequestLoad();

                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
                SceneManager.LoadScene("CharacterSelectScene");
            }
            else
            {
                SetStatus(result.message);
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
        }

        [System.Serializable]
        public class LoginRequest
        {
            public string username;
            public string password;
            public bool isRegister;
            public string clientVersion;
        }

        [System.Serializable]
        public class LoginResponse
        {
            public bool success;
            public string playerId;
            public string message;
        }
    }
}
