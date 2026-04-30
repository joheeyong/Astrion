using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    public class GameHUD : MonoBehaviour
    {
        // References set by ProjectSetup
        [SerializeField] private Image hpFill;
        [SerializeField] private Image mpFill;
        [SerializeField] private Text hpText;
        [SerializeField] private Text mpText;
        [SerializeField] private Text charNameText;
        [SerializeField] private Text charLevelText;
        [SerializeField] private Text coordsText;
        [SerializeField] private RawImage minimapImage;

        private Transform _player;
        private float _maxHp = 100f;
        private float _currentHp = 100f;
        private float _maxMp = 50f;
        private float _currentMp = 50f;
        private Camera _minimapCam;

        private void Start()
        {
            string charName = PlayerPrefs.GetString("characterName", "Adventurer");
            string charClass = PlayerPrefs.GetString("characterClass", "Warrior");
            if (charNameText) charNameText.text = charName;
            if (charLevelText) charLevelText.text = $"Lv.1 {charClass}";
            UpdateBars();
        }

        private void LateUpdate()
        {
            if (_player == null)
            {
                var p = GameObject.Find("PlayerPrefab");
                if (p == null) p = GameObject.Find("LocalPlayer");
                if (p != null) _player = p.transform;
                else return;
            }

            // Update coordinates
            if (coordsText)
            {
                var pos = _player.position;
                coordsText.text = $"X:{pos.x:F0}  Z:{pos.z:F0}";
            }

            // Update minimap camera position
            if (_minimapCam != null)
            {
                var pos = _player.position;
                _minimapCam.transform.position = new Vector3(pos.x, pos.y + 80, pos.z);
            }
        }

        public void SetMinimapCamera(Camera cam)
        {
            _minimapCam = cam;
        }

        private void UpdateBars()
        {
            if (hpFill) hpFill.fillAmount = _currentHp / _maxHp;
            if (mpFill) mpFill.fillAmount = _currentMp / _maxMp;
            if (hpText) hpText.text = $"{(int)_currentHp}/{(int)_maxHp}";
            if (mpText) mpText.text = $"{(int)_currentMp}/{(int)_maxMp}";
        }
    }
}
