using System;
using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [SerializeField] private GameObject hintPanel;
        [SerializeField] private Text hintText;
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text contentText;
        [SerializeField] private Text continuePrompt;

        private string[] _lines;
        private int _index;
        private bool _isOpen;
        private Action _onEnd;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            Instance = this;
            if (hintPanel) hintPanel.SetActive(false);
            if (dialogPanel) dialogPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowHint(string npcName)
        {
            if (_isOpen) return;
            if (hintPanel) hintPanel.SetActive(true);
            if (hintText) hintText.text = $"[E]  {npcName}와 대화";
        }

        public void HideHint()
        {
            if (hintPanel) hintPanel.SetActive(false);
        }

        public void StartDialogue(string speaker, string[] lines, Action onEnd = null)
        {
            if (lines == null || lines.Length == 0) return;
            _lines = lines;
            _index = 0;
            _isOpen = true;
            _onEnd = onEnd;
            if (hintPanel) hintPanel.SetActive(false);
            if (dialogPanel) dialogPanel.SetActive(true);
            if (speakerText) speakerText.text = speaker;
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            if (contentText != null && _index < _lines.Length)
                contentText.text = _lines[_index];
            if (continuePrompt)
                continuePrompt.text = _index < _lines.Length - 1 ? "[Space]  계속" : "[Space]  닫기";
        }

        private void Advance()
        {
            _index++;
            if (_index >= _lines.Length) Close();
            else ShowCurrent();
        }

        private void Close()
        {
            var cb = _onEnd;
            _onEnd = null;
            _isOpen = false;
            if (dialogPanel) dialogPanel.SetActive(false);
            cb?.Invoke();
        }

        private void Update()
        {
            if (!_isOpen) return;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                Advance();
            else if (Input.GetKeyDown(KeyCode.Escape))
                Close();
        }
    }
}
