using System.Collections.Generic;
using Astrion.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astrion.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private Transform slotContainer;
        [SerializeField] private Button enterButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Text selectedInfoName;
        [SerializeField] private Text selectedInfoDetail;

        private int _selectedIndex = -1;
        private List<CharacterData> _characters = new();
        private bool _loaded;

        private void Start()
        {
            enterButton.onClick.AddListener(OnEnterClicked);
            createButton.onClick.AddListener(OnCreateClicked);
            enterButton.interactable = false;

            NetworkManager.Instance.OnPacketReceived += HandlePacket;

            // Request character list from server
            RequestCharacterList();
        }

        private void RequestCharacterList()
        {
            if (selectedInfoDetail) selectedInfoDetail.text = "Loading characters...";
            NetworkManager.Instance.SendPacket(PacketType.CharacterList, "{}");
        }

        private void HandlePacket(GamePacket packet)
        {
            if (packet.Type == PacketType.CharacterListResult)
            {
                var result = JsonUtility.FromJson<CharacterListResponse>(packet.Payload);
                _characters.Clear();
                if (result.characters != null)
                {
                    foreach (var c in result.characters)
                    {
                        if (!string.IsNullOrEmpty(c.name))
                            _characters.Add(c);
                    }
                }
                _loaded = true;
                _selectedIndex = -1;
                enterButton.interactable = false;
                RefreshSlots();
            }
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < slotContainer.childCount; i++)
            {
                var slot = slotContainer.GetChild(i);
                var nameText = slot.Find("Name")?.GetComponent<Text>();
                var classText = slot.Find("Class")?.GetComponent<Text>();
                var levelText = slot.Find("Level")?.GetComponent<Text>();
                var emptyText = slot.Find("Empty")?.GetComponent<Text>();
                var highlight = slot.Find("Highlight")?.GetComponent<Image>();

                if (i < _characters.Count)
                {
                    var c = _characters[i];
                    if (nameText) { nameText.text = c.name; nameText.gameObject.SetActive(true); }
                    if (classText) { classText.text = c.className; classText.gameObject.SetActive(true); }
                    if (levelText) { levelText.text = $"Lv. {c.level}"; levelText.gameObject.SetActive(true); }
                    if (emptyText) emptyText.gameObject.SetActive(false);
                }
                else
                {
                    if (nameText) nameText.gameObject.SetActive(false);
                    if (classText) classText.gameObject.SetActive(false);
                    if (levelText) levelText.gameObject.SetActive(false);
                    if (emptyText) { emptyText.text = "Empty Slot"; emptyText.gameObject.SetActive(true); }
                }

                if (highlight) highlight.color = Color.clear;

                int idx = i;
                var btn = slot.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    if (i < _characters.Count)
                        btn.onClick.AddListener(() => SelectSlot(idx));
                }
            }

            bool hasChars = _characters.Count > 0;
            createButton.gameObject.SetActive(true);
            selectedInfoName.text = hasChars ? "Select a character" : "";
            selectedInfoDetail.text = hasChars ? "" : "No characters yet. Create one to begin!";
        }

        private void SelectSlot(int index)
        {
            _selectedIndex = index;
            enterButton.interactable = true;

            var c = _characters[index];
            selectedInfoName.text = c.name;
            selectedInfoDetail.text = $"{c.className}  |  Level {c.level}";

            for (int i = 0; i < slotContainer.childCount; i++)
            {
                var highlight = slotContainer.GetChild(i).Find("Highlight")?.GetComponent<Image>();
                if (highlight)
                    highlight.color = i == index
                        ? new Color(0.85f, 0.72f, 0.40f, 0.3f)
                        : Color.clear;
            }
        }

        private void OnEnterClicked()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _characters.Count) return;
            var c = _characters[_selectedIndex];
            PlayerPrefs.SetString("characterName", c.name);
            PlayerPrefs.SetString("characterClass", c.className);
            PlayerPrefs.Save();
            NetworkManager.Instance.OnPacketReceived -= HandlePacket;
            SceneManager.LoadScene("MainScene");
        }

        private void OnCreateClicked()
        {
            NetworkManager.Instance.OnPacketReceived -= HandlePacket;
            SceneManager.LoadScene("CharacterCreateScene");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
        }

        [System.Serializable]
        public class CharacterData
        {
            public string name;
            public string className;
            public int level;
        }

        [System.Serializable]
        public class CharacterListResponse
        {
            public CharacterData[] characters;
        }
    }
}
