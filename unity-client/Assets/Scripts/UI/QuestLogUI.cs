using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Astrion.Game;
using Astrion.Network;

namespace Astrion.UI
{
    public class QuestLogUI : MonoBehaviour
    {
        public static QuestLogUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private Transform activeRoot;
        [SerializeField] private Transform completedRoot;
        [SerializeField] private Text emptyText;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject entryTemplate;
        [SerializeField] private KeyCode toggleKey = KeyCode.J;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (panel) panel.SetActive(false);
            if (entryTemplate) entryTemplate.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (IsTextInputFocused()) return;
            if (Input.GetKeyDown(toggleKey)) Toggle();
            else if (Input.GetKeyDown(KeyCode.Escape) && IsOpen) Close();
        }

        private bool IsTextInputFocused()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            return sel != null && sel.GetComponent<InputField>() != null;
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            if (panel == null) return;
            panel.SetActive(true);
            Rebuild();
        }

        public void Close() { if (panel) panel.SetActive(false); }

        private void Rebuild()
        {
            // Clear previous entries
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();

            var psm = PlayerStateManager.Instance;
            if (psm == null || psm.State == null)
            {
                if (emptyText) emptyText.gameObject.SetActive(true);
                return;
            }
            var state = psm.State;

            int total = 0;

            // Active quest (single-slot for now — QuestSystem only tracks one)
            if (!string.IsNullOrEmpty(state.questId) && state.questState != 2 /*Complete-but-not-turned-in*/)
            {
                // questState: 0=NotStarted, 1=InProgress, 2=Complete
                if (state.questState >= 1)
                {
                    SpawnEntry(activeRoot, state.questId, state.questTitle,
                        $"{state.questProgress} / {state.questTarget}",
                        state.questState == 2 ? "완료 (보고 가능)" : "진행 중",
                        active: true);
                    total++;
                }
            }
            else if (!string.IsNullOrEmpty(state.questId) && state.questState == 2)
            {
                SpawnEntry(activeRoot, state.questId, state.questTitle,
                    $"{state.questProgress} / {state.questTarget}",
                    "완료 (보고 가능)", active: true);
                total++;
            }

            // Completed quests
            if (state.completedQuestIds != null)
            {
                foreach (var id in state.completedQuestIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    string title = QuestDatabase.TitleOf(id);
                    SpawnEntry(completedRoot, id, title, "", "완료", active: false);
                    total++;
                }
            }

            if (emptyText) emptyText.gameObject.SetActive(total == 0);
        }

        private void SpawnEntry(Transform parent, string questId, string title, string progress, string status, bool active)
        {
            if (entryTemplate == null || parent == null) return;
            var go = Instantiate(entryTemplate, parent);
            go.name = $"Entry_{questId}";
            go.SetActive(true);

            var nameT = go.transform.Find("Title")?.GetComponent<Text>();
            if (nameT != null)
            {
                nameT.text = title;
                nameT.color = active
                    ? new Color(1f, 0.92f, 0.45f)         // gold for in-progress
                    : new Color(0.62f, 0.58f, 0.50f);    // muted gray for done
            }

            var statusT = go.transform.Find("Status")?.GetComponent<Text>();
            if (statusT != null)
            {
                statusT.text = string.IsNullOrEmpty(progress) ? status : $"{status}  ·  {progress}";
                statusT.color = active
                    ? new Color(0.78f, 0.72f, 0.55f)
                    : new Color(0.45f, 0.42f, 0.36f);
            }

            // Optional summary
            var summaryT = go.transform.Find("Summary")?.GetComponent<Text>();
            if (summaryT != null)
            {
                var def = QuestDatabase.Get(questId);
                summaryT.text = def != null ? def.summary : "";
                summaryT.color = active
                    ? new Color(0.78f, 0.72f, 0.55f)
                    : new Color(0.45f, 0.42f, 0.36f);
            }

            _spawned.Add(go);
        }
    }
}
