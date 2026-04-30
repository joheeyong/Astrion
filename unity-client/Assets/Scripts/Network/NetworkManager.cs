using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Astrion.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Server Settings")]
        [SerializeField] private string serverHost = "3.38.109.138";
        [SerializeField] private int serverPort = 9000;
        [SerializeField] private int maxRetries = 5;
        [SerializeField] private float retryDelaySeconds = 2f;

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isConnected;

        private readonly ConcurrentQueue<GamePacket> _receiveQueue = new();
        private readonly ConcurrentQueue<Action> _mainThreadActions = new();

        public event Action<GamePacket> OnPacketReceived;
        public event Action OnConnected;
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
            new Thread(() => ConnectWithRetry()).Start();
        }

        private void ConnectWithRetry()
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.Log($"[Network] Connecting to {serverHost}:{serverPort} (attempt {attempt}/{maxRetries})...");
                    _client = new TcpClient();
                    _client.NoDelay = true;
                    _client.Connect(serverHost, serverPort);
                    _stream = _client.GetStream();
                    _isConnected = true;

                    _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                    _receiveThread.Start();

                    Debug.Log($"[Network] Connected to {serverHost}:{serverPort}");
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
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            Debug.Log("[Network] Disconnected");
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}
