using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Astrion.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        // SHA-256 fingerprint of the server's TLS certificate (DER bytes).
        // Pinned here — even if a CA-signed cert claims to be our server,
        // we reject it unless this exact hash matches. Update on cert rotation.
        // Server cmd to print: openssl x509 -in server.crt -noout -fingerprint -sha256
        private const string ServerCertSha256 =
            "1EEA59A85846E2450BA226E03141113B72B16F2171D8C986734B3F94CA569DDE";

        [Header("Server Settings (overridden by NetworkConfig at runtime)")]
        [SerializeField] private int maxRetries = 5;
        [SerializeField] private float retryDelaySeconds = 2f;

        private TcpClient _client;
        private Stream _stream;  // SslStream over the raw NetworkStream
        private Thread _receiveThread;
        private bool _isConnected;

        private readonly ConcurrentQueue<GamePacket> _receiveQueue = new();
        private readonly ConcurrentQueue<Action> _mainThreadActions = new();

        public event Action<GamePacket> OnPacketReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public bool IsConnected => _isConnected;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Connect()
        {
            // PlayerPrefs is main-thread-only in Unity, so resolve host/port
            // HERE on the caller's main thread, then hand the captured values
            // to the worker. The Connect() entry points are all called from
            // UI / scene lifecycle code, which runs on the main thread.
            string host = NetworkConfig.Host;
            int port = NetworkConfig.Port;
            new Thread(() => ConnectWithRetry(host, port)).Start();
        }

        private void ConnectWithRetry(string host, int port)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.Log($"[Network] Connecting to {host}:{port} (attempt {attempt}/{maxRetries})...");
                    _client = new TcpClient();
                    _client.NoDelay = true;
                    _client.Connect(host, port);

                    var rawStream = _client.GetStream();
#if ASTRION_DEV
                    // Dev build talks to the operator's local mac server,
                    // which runs without TLS to keep the dev loop quick
                    // (no cert provisioning per developer). The connection
                    // is localhost-only, never crosses the wire. Prod
                    // builds (without the ASTRION_DEV define) keep the
                    // full TLS+fingerprint-pinning path below.
                    _stream = rawStream;
#else
                    // Wrap the raw socket in TLS. The targetHost arg is only
                    // used for SNI / hostname matching, which we override via
                    // ValidateServerCert (we pin on fingerprint, not CN/SAN),
                    // so any non-empty string here is fine.
                    var ssl = new SslStream(rawStream, leaveInnerStreamOpen: false,
                        userCertificateValidationCallback: ValidateServerCert);
                    ssl.AuthenticateAsClient(host);
                    _stream = ssl;
#endif
                    _isConnected = true;

                    _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                    _receiveThread.Start();

                    Debug.Log($"[Network] Connected to {host}:{port}");
                    _mainThreadActions.Enqueue(() => OnConnected?.Invoke());
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Network] Attempt {attempt} failed: {e.Message}");
                    if (attempt < maxRetries)
                        Thread.Sleep((int)(retryDelaySeconds * 1000));
                }
            }
            Debug.LogError($"[Network] Failed to connect after {maxRetries} attempts");
        }

        public void SendPacket(PacketType type, string jsonPayload)
        {
            if (!_isConnected) return;

            try
            {
                byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
                int length = 1 + payloadBytes.Length;

                byte[] lengthBytes = BitConverter.GetBytes(length);
                if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes); // Big-endian

                _stream.Write(lengthBytes, 0, 4);
                _stream.WriteByte((byte)type);
                _stream.Write(payloadBytes, 0, payloadBytes.Length);
                _stream.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Send failed: {e.Message}");
                Disconnect();
            }
        }

        private void ReceiveLoop()
        {
            byte[] headerBuffer = new byte[4];

            try
            {
                while (_isConnected)
                {
                    // Read length (4 bytes, big-endian)
                    if (!ReadExact(headerBuffer, 4)) break;
                    if (BitConverter.IsLittleEndian) Array.Reverse(headerBuffer);
                    int length = BitConverter.ToInt32(headerBuffer, 0);

                    // Read packet type (1 byte) + payload
                    byte[] data = new byte[length];
                    if (!ReadExact(data, length)) break;

                    PacketType type = (PacketType)data[0];
                    string payload = Encoding.UTF8.GetString(data, 1, length - 1);

                    _receiveQueue.Enqueue(new GamePacket(type, payload));
                }
            }
            catch (Exception e)
            {
                if (_isConnected)
                    Debug.LogError($"[Network] Receive error: {e.Message}");
            }
            // Receive loop exited — make sure the rest of the app knows
            if (_isConnected) Disconnect();
        }

        // Reject any cert whose DER-encoded SHA-256 doesn't match the pin,
        // regardless of CN/SAN, expiry, or chain trust. A MITM presenting a
        // 'valid' cert from a real CA still loses here.
        private static bool ValidateServerCert(object sender, X509Certificate cert,
            X509Chain chain, SslPolicyErrors errors)
        {
            if (cert == null) return false;
            byte[] der = cert.GetRawCertData();
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(der);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("X2"));
            string actual = sb.ToString();
            string expected = ServerCertSha256.Replace(":", "").ToUpperInvariant();
            bool ok = actual.Equals(expected, StringComparison.Ordinal);
            if (!ok)
                Debug.LogError($"[Network] TLS cert pin MISMATCH. expected={expected} actual={actual}");
            return ok;
        }

        private bool ReadExact(byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = _stream.Read(buffer, offset, count - offset);
                if (read <= 0) return false;
                offset += read;
            }
            return true;
        }

        private void Update()
        {
            while (_mainThreadActions.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
            while (_receiveQueue.TryDequeue(out GamePacket packet))
            {
                OnPacketReceived?.Invoke(packet);
            }
        }

        public void Disconnect()
        {
            bool wasConnected = _isConnected;
            _isConnected = false;
            try { _stream?.Close(); } catch { /* ignore */ }
            try { _client?.Close(); } catch { /* ignore */ }
            _stream = null;
            _client = null;
            Debug.Log("[Network] Disconnected");
            if (wasConnected)
                _mainThreadActions.Enqueue(() => OnDisconnected?.Invoke());
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}
