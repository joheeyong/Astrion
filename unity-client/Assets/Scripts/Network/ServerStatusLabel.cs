using UnityEngine;
using UnityEngine.UI;

namespace Astrion.Network
{
    /// Tiny "Server: host:port" label on the login screen so it's visible
    /// what the build is currently pointing at.
    [RequireComponent(typeof(Text))]
    public class ServerStatusLabel : MonoBehaviour
    {
        private Text _text;
        private void Awake() { _text = GetComponent<Text>(); }
        private void OnEnable() { Refresh(); }
        private void Refresh()
        {
            if (_text != null) _text.text = "Server: " + NetworkConfig.DisplayString;
        }
    }
}
