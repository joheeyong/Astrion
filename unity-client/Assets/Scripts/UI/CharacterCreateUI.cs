using Astrion.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astrion.UI
{
    public class CharacterCreateUI : MonoBehaviour
    {
        [SerializeField] private InputField nameInput;
        [SerializeField] private Button[] classButtons;
        [SerializeField] private Image[] classHighlights;
        [SerializeField] private Text selectedClassName;
        [SerializeField] private Text selectedClassDesc;
        [SerializeField] private Button createButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Text statusText;

        private int _selectedClass = -1;
        private bool _waitingForResponse;

        private static readonly string[] ClassNames = { "Warrior", "Mage", "Archer" };
        private static readonly string[] ClassDescs = {
            "전사 — 검을 휘둘러 적을 베는 근접 전투원.\n시작: 수련의 검 · STR 10",
            "마법사 — 별빛을 다루는 학자.\n시작: 수련의 지팡이 · INT 10 · MP 70",
            "궁수 — 활을 다루는 사냥꾼.\n시작: 옛 별의 활 · DEX 10"
        };

        private void Start()
        {
            for (int i = 0; i < classButtons.Length; i++)
            {
                int idx = i;
                classButtons[i].onClick.AddListener(() => SelectClass(idx));
            }

            createButton.onClick.AddListener(OnCreateClicked);
            backButton.onClick.AddListener(OnBackClicked);
            createButton.interactable = false;

            selectedClassName.text = "";
            selectedClassDesc.text = "Choose a class";
            statusText.text = "";

            NetworkManager.Instance.OnPacketReceived += HandlePacket;
        }

        private void SelectClass(int index)
        {
            _selectedClass = index;
            selectedClassName.text = ClassNames[index];
            selectedClassDesc.text = ClassDescs[index];
            createButton.interactable = !string.IsNullOrWhiteSpace(nameInput.text);

            for (int i = 0; i < classHighlights.Length; i++)
                classHighlights[i].color = i == index
                    ? new Color(0.85f, 0.72f, 0.40f, 0.35f)
                    : Color.clear;
        }

        private void Update()
        {
            if (_selectedClass >= 0 && !_waitingForResponse)
                createButton.interactable = !string.IsNullOrWhiteSpace(nameInput.text);
        }

        private void OnCreateClicked()
        {
            if (_waitingForResponse) return;

            string charName = nameInput.text.Trim();
            if (string.IsNullOrEmpty(charName))
            {
                statusText.text = "Enter a name for your character.";
                return;
            }
            if (_selectedClass < 0)
            {
                statusText.text = "Select a class.";
                return;
            }
            if (charName.Length < 2 || charName.Length > 16)
            {
                statusText.text = "Name must be 2-16 characters.";
                return;
            }

            _waitingForResponse = true;
            createButton.interactable = false;
            statusText.text = "Creating character...";

            string payload = JsonUtility.ToJson(new CharacterCreateRequest
            {
                name = charName,
                className = ClassNames[_selectedClass]
            });
            NetworkManager.Instance.SendPacket(PacketType.CharacterCreate, payload);
        }

        private void HandlePacket(GamePacket packet)
        {
            if (packet.Type != PacketType.CharacterCreateResult) return;

            var result = JsonUtility.FromJson<CharacterCreateResponse>(packet.Payload);
            _waitingForResponse = false;

            if (result.success)
            {
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
                SceneManager.LoadScene("CharacterSelectScene");
            }
            else
            {
                statusText.text = result.message;
                createButton.interactable = true;
            }
        }

        private void OnBackClicked()
        {
            NetworkManager.Instance.OnPacketReceived -= HandlePacket;
            SceneManager.LoadScene("CharacterSelectScene");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
        }

        [System.Serializable]
        public class CharacterCreateRequest
        {
            public string name;
            public string className;
        }

        [System.Serializable]
        public class CharacterCreateResponse
        {
            public bool success;
            public string message;
        }
    }
}
