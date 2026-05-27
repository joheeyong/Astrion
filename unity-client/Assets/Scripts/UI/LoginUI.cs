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
        [SerializeField] private Toggle autoLoginToggle;

        private bool _waitingForResponse;
        // Tracks whether the current in-flight login was initiated by the
        // auto-login flow (passwordInput is empty in that case). If the
        // server rejects it, we wipe the stored hash so we don't loop on a
        // stale credential.
        private bool _autoLoginAttempt;

        private const string PrefAutoLogin     = "autoLogin";
        private const string PrefAutoLoginUser = "autoLoginUser";
        private const string PrefAutoLoginHash = "autoLoginHash";

        private void Start()
        {
            string saved = PlayerPrefs.GetString("username", "");
            if (!string.IsNullOrEmpty(saved))
                usernameInput.text = saved;

            if (autoLoginToggle != null)
                autoLoginToggle.isOn = PlayerPrefs.GetInt(PrefAutoLogin, 0) == 1;

            loginButton.onClick.AddListener(OnLoginClicked);
            registerButton.onClick.AddListener(OnRegisterClicked);

            NetworkManager.Instance.OnPacketReceived += HandlePacket;

            // If the toggle is on AND we have a credential saved for the
            // currently-displayed username, kick off auto-login immediately.
            string savedUser = PlayerPrefs.GetString(PrefAutoLoginUser, "");
            string savedHash = PlayerPrefs.GetString(PrefAutoLoginHash, "");
            if (autoLoginToggle != null && autoLoginToggle.isOn
                && !string.IsNullOrEmpty(savedUser) && !string.IsNullOrEmpty(savedHash)
                && savedUser == saved)
            {
                TryAutoLogin(savedUser, savedHash);
            }
        }

        private void TryAutoLogin(string username, string passwordHash)
        {
            SetStatus("Auto-logging in...");
            _waitingForResponse = true;
            _autoLoginAttempt = true;

            if (NetworkManager.Instance.IsConnected)
            {
                SendLoginHashed(username, passwordHash, false);
            }
            else
            {
                NetworkManager.Instance.OnConnected += () => SendLoginHashed(username, passwordHash, false);
                NetworkManager.Instance.Connect();
            }
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
            _autoLoginAttempt = false;

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
            _autoLoginAttempt = false;

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
            // Hash on the client. Server expects the digest, not the plaintext.
            string hashed = Astrion.Network.PasswordHasher.Sha256Hex(password);
            SendLoginHashed(username, hashed, isRegister);
        }

        private void SendLoginHashed(string username, string passwordHash, bool isRegister)
        {
            SetStatus(isRegister ? "Registering..." : "Logging in...");
            string payload = JsonUtility.ToJson(new LoginRequest
            {
                username = username,
                password = passwordHash,
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
                string user = usernameInput.text.Trim();
                PlayerPrefs.SetString("username", user);
                PlayerPrefs.SetString("playerId", result.playerId);

                // Persist auto-login decision. We only have a fresh hash when
                // the player just typed a password — on the auto-login round
                // trip passwordInput is empty, so leave the stored hash alone.
                bool autoOn = autoLoginToggle != null && autoLoginToggle.isOn;
                if (autoOn && !string.IsNullOrEmpty(passwordInput.text))
                {
                    string hashed = Astrion.Network.PasswordHasher.Sha256Hex(passwordInput.text);
                    PlayerPrefs.SetInt(PrefAutoLogin, 1);
                    PlayerPrefs.SetString(PrefAutoLoginUser, user);
                    PlayerPrefs.SetString(PrefAutoLoginHash, hashed);
                }
                else if (!autoOn)
                {
                    PlayerPrefs.SetInt(PrefAutoLogin, 0);
                    PlayerPrefs.DeleteKey(PrefAutoLoginUser);
                    PlayerPrefs.DeleteKey(PrefAutoLoginHash);
                }
                PlayerPrefs.Save();

                // Cache credentials in-memory for auto-reconnect (not persisted).
                Astrion.Network.SessionCredentials.Username = user;
                Astrion.Network.SessionCredentials.Password =
                    string.IsNullOrEmpty(passwordInput.text)
                        ? PlayerPrefs.GetString(PrefAutoLoginHash, "")
                        : Astrion.Network.PasswordHasher.Sha256Hex(passwordInput.text);

                PlayerStateManager.Instance?.RequestLoad();

                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
                SceneManager.LoadScene("CharacterSelectScene");
            }
            else
            {
                // If the rejected attempt was an auto-login, the stored hash
                // is stale (password changed, account locked, etc.). Wipe it
                // and disable the toggle so the player can re-enter manually.
                if (_autoLoginAttempt)
                {
                    PlayerPrefs.SetInt(PrefAutoLogin, 0);
                    PlayerPrefs.DeleteKey(PrefAutoLoginHash);
                    PlayerPrefs.Save();
                    if (autoLoginToggle != null) autoLoginToggle.isOn = false;
                    SetStatus("Auto-login failed: " + result.message);
                }
                else
                {
                    SetStatus(result.message);
                }
                _autoLoginAttempt = false;
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
