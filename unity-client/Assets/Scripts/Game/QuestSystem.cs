using System;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        public enum State { NotStarted, InProgress, Complete }

        public string CurrentQuestId { get; private set; }
        public string CurrentQuestTitle { get; private set; }
        public int Progress { get; private set; }
        public int Target { get; private set; }
        public State CurrentState { get; private set; } = State.NotStarted;

        public event Action OnChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            var psm = PlayerStateManager.Instance;
            if (psm != null)
            {
                if (psm.IsLoaded) RestoreFromState();
                else psm.OnLoaded += RestoreFromState;
            }
        }

        private void OnDestroy()
        {
            var psm = PlayerStateManager.Instance;
            if (psm != null) psm.OnLoaded -= RestoreFromState;
            if (Instance == this) Instance = null;
        }

        private void RestoreFromState()
        {
            var s = PlayerStateManager.Instance?.State;
            if (s == null || string.IsNullOrEmpty(s.questId)) return;
            CurrentQuestId = s.questId;
            CurrentQuestTitle = s.questTitle;
            Progress = s.questProgress;
            Target = s.questTarget;
            CurrentState = (State)s.questState;
            OnChanged?.Invoke();
        }

        public State StateOf(string id)
        {
            if (CurrentQuestId != id) return State.NotStarted;
            return CurrentState;
        }

        public void StartQuest(string id, string title, int target)
        {
            CurrentQuestId = id;
            CurrentQuestTitle = title;
            Target = Mathf.Max(1, target);
            Progress = 0;
            CurrentState = State.InProgress;
            PlayerStateManager.Instance?.UpdateQuestState(id, title, Progress, Target, (int)CurrentState);
            OnChanged?.Invoke();
        }

        public void AddProgress(string id, int amount = 1)
        {
            if (CurrentQuestId != id || CurrentState != State.InProgress) return;
            Progress = Mathf.Min(Progress + amount, Target);
            if (Progress >= Target) CurrentState = State.Complete;
            PlayerStateManager.Instance?.UpdateQuestState(CurrentQuestId, CurrentQuestTitle, Progress, Target, (int)CurrentState);
            OnChanged?.Invoke();
        }

        public void TurnIn(string id)
        {
            if (CurrentQuestId != id || CurrentState != State.Complete) return;
            CurrentQuestId = null;
            CurrentQuestTitle = null;
            CurrentState = State.NotStarted;
            Progress = 0;
            Target = 0;
            PlayerStateManager.Instance?.MarkQuestCompleted(id);
            PlayerStateManager.Instance?.ClearAfterTurnIn();
            OnChanged?.Invoke();
        }
    }
}
