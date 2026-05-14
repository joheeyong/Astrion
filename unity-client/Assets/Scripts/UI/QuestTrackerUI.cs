using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    public class QuestTrackerUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text progressText;
        [SerializeField] private Color inProgressColor = new Color(0.92f, 0.88f, 0.55f);
        [SerializeField] private Color completeColor = new Color(0.55f, 1f, 0.55f);

        private void Start()
        {
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.OnChanged += Refresh;
                Refresh();
            }
            else if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (QuestSystem.Instance != null)
                QuestSystem.Instance.OnChanged -= Refresh;
        }

        private void Refresh()
        {
            var qs = QuestSystem.Instance;
            if (qs == null || string.IsNullOrEmpty(qs.CurrentQuestId))
            {
                if (panel) panel.SetActive(false);
                return;
            }
            if (panel) panel.SetActive(true);
            if (titleText) titleText.text = qs.CurrentQuestTitle;
            if (progressText)
            {
                progressText.text = $"{qs.Progress} / {qs.Target}";
                progressText.color = qs.CurrentState == QuestSystem.State.Complete
                    ? completeColor : inProgressColor;
            }
        }
    }
}
