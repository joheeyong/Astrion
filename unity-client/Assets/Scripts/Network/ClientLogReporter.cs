using System;
using UnityEngine;

namespace Astrion.Network
{
    /// Captures every client-side Exception / Error and ships it to the server
    /// over the existing TLS channel. Without this, NREs and other client
    /// failures only exist in the user's local Player.log — invisible to the
    /// operator and unrecoverable once the user closes the game.
    ///
    /// Self-throttled so an Exception inside Update (which fires every frame)
    /// can't flood the network: at most {MaxPerInterval} messages per
    /// {IntervalSeconds}. Excess events are dropped silently — the operator
    /// will see the first occurrence and the (truncated) stack, which is
    /// enough to triage.
    public class ClientLogReporter : MonoBehaviour
    {
        private const int   MaxPerInterval  = 10;     // per IntervalSeconds
        private const float IntervalSeconds = 30f;

        private float _windowStart;
        private int   _sentInWindow;
        private bool  _suppressLoop;  // re-entrancy guard

        private void OnEnable()  { Application.logMessageReceived += OnLog; }
        private void OnDisable() { Application.logMessageReceived -= OnLog; }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            // Only forward serious events. Warning/Log/Info would drown the channel.
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            if (_suppressLoop) return;

            // Per-window throttle. Reset window on first event after expiry.
            float now = Time.unscaledTime;
            if (now - _windowStart > IntervalSeconds)
            {
                _windowStart = now;
                _sentInWindow = 0;
            }
            if (_sentInWindow >= MaxPerInterval) return;
            _sentInWindow++;

            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;

            _suppressLoop = true;
            try
            {
                // Trim — a runaway stack can be tens of KB; the operator only
                // needs the top frames to locate the call site.
                string msg   = Truncate(condition, 600);
                string stack = Truncate(stackTrace, 2000);

                string payload = JsonUtility.ToJson(new Payload {
                    level   = type.ToString(),
                    message = msg,
                    stackTrace = stack,
                });
                nm.SendPacket(PacketType.ClientLog, payload);
            }
            catch
            {
                // Whatever went wrong during send must not produce another
                // log event that retriggers OnLog. Eat it.
            }
            finally { _suppressLoop = false; }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "...(truncated)";
        }

        [Serializable] private class Payload
        {
            public string level;
            public string message;
            public string stackTrace;
        }
    }
}
