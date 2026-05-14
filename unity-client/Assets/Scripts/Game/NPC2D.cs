using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    [System.Serializable]
    public class NPCQuestStage
    {
        public string questId;
        public string questTitle;
        public int questTarget = 1;
        [TextArea(2, 6)] public string[] introLines;
        [TextArea(2, 6)] public string[] reminderLines;
        [TextArea(2, 6)] public string[] completionLines;
    }

    [RequireComponent(typeof(Collider2D))]
    public class NPC2D : MonoBehaviour
    {
        [SerializeField] private string npcName = "NPC";
        [SerializeField] private NPCQuestStage[] questStages;
        [SerializeField, TextArea(2, 4)] private string[] idleLines;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private bool _playerNearby;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null)
            {
                _playerNearby = true;
                Astrion.UI.DialogueUI.Instance?.ShowHint(npcName);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null)
            {
                _playerNearby = false;
                Astrion.UI.DialogueUI.Instance?.HideHint();
            }
        }

        private void Update()
        {
            if (!_playerNearby) return;
            var dlg = Astrion.UI.DialogueUI.Instance;
            if (dlg == null || dlg.IsOpen) return;
            if (Input.GetKeyDown(interactKey)) Interact();
        }

        private void Interact()
        {
            var dlg = Astrion.UI.DialogueUI.Instance;
            var qs = QuestSystem.Instance;
            var psm = PlayerStateManager.Instance;

            if (questStages != null)
            {
                for (int i = 0; i < questStages.Length; i++)
                {
                    var stage = questStages[i];
                    if (stage == null || string.IsNullOrEmpty(stage.questId)) continue;
                    if (psm != null && psm.IsQuestCompleted(stage.questId)) continue;

                    var state = qs != null ? qs.StateOf(stage.questId) : QuestSystem.State.NotStarted;
                    switch (state)
                    {
                        case QuestSystem.State.NotStarted:
                            dlg.StartDialogue(npcName, stage.introLines, () => StartStage(stage));
                            return;
                        case QuestSystem.State.InProgress:
                            if (stage.reminderLines != null && stage.reminderLines.Length > 0)
                                dlg.StartDialogue(npcName, stage.reminderLines);
                            return;
                        case QuestSystem.State.Complete:
                            dlg.StartDialogue(npcName, stage.completionLines, () => TurnInStage(stage));
                            return;
                    }
                }
            }

            // All stages done — idle line
            if (idleLines != null && idleLines.Length > 0)
                dlg.StartDialogue(npcName, idleLines);
        }

        private void StartStage(NPCQuestStage stage)
        {
            QuestSystem.Instance?.StartQuest(stage.questId, stage.questTitle, stage.questTarget);
        }

        private void TurnInStage(NPCQuestStage stage)
        {
            QuestSystem.Instance?.TurnIn(stage.questId);
        }
    }
}
