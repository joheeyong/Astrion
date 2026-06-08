using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astrion.Game
{
    /// First-run guided tour through the basic controls + UI keys. Sits
    /// in the bottom-center of the screen as a small unobtrusive panel
    /// that auto-advances when the player performs each step's action.
    /// Triggered automatically once on first game-scene entry; replayable
    /// any time via F1.
    ///
    /// DDOL singleton on networkGo, paired with TutorialUI for rendering.
    public class TutorialSystem : MonoBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        // Bumped suffix (_v1 → _v2) when the step list materially changes,
        // so players who've 'done' the old one see the new flow.
        private const string PrefDone = "tutorial_done_v1";

        public class Step
        {
            public string label;
            public Func<bool> detect;
        }

        private readonly List<Step> _steps = new();
        private int _current = -1;

        public int Current => _current;
        public int Total => _steps.Count;
        public bool Active => _current >= 0 && _current < _steps.Count;
        public string CurrentLabel => Active ? _steps[_current].label : "";
        public event Action OnUpdated;

        // ─────────────────────── lifecycle ───────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSteps();
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (Instance == this) Instance = null;
        }

        private void BuildSteps()
        {
            _steps.Clear();
            _steps.Add(new Step { label = "WASD 또는 화살표 키로 이동해보세요.",         detect = DetectMove });
            _steps.Add(new Step { label = "Space 키로 점프해보세요.",                       detect = DetectJump });
            _steps.Add(new Step { label = "Ctrl 또는 마우스 좌클릭으로 공격합니다.",         detect = DetectAttack });
            _steps.Add(new Step { label = "[I] 키로 인벤토리를 엽니다.",                    detect = DetectInventory });
            _steps.Add(new Step { label = "[1] / [2] 키로 HP / MP 포션을 사용합니다.",       detect = DetectPotion });
            _steps.Add(new Step { label = "[M] 키로 월드맵을 열어 위치를 확인합니다.",       detect = DetectWorldmap });
        }

        // First entry into any actual gameplay scene kicks off the tour.
        // 'Done' flag survives both relogs and scene transitions.
        private void OnSceneChanged(Scene prev, Scene next)
        {
            if (!IsGameScene(next.name)) return;
            if (Active) return;
            if (PlayerPrefs.GetInt(PrefDone, 0) == 1) return;
            // Defer a frame so the player has actually loaded in.
            StartCoroutine(StartAfterFrame());
        }

        private System.Collections.IEnumerator StartAfterFrame()
        {
            yield return null;
            yield return new WaitForSeconds(1.2f);
            StartTutorial();
        }

        // ─────────────────────── public API ───────────────────────

        public void StartTutorial()
        {
            _current = 0;
            OnUpdated?.Invoke();
        }

        public void Reset()
        {
            PlayerPrefs.SetInt(PrefDone, 0);
            PlayerPrefs.Save();
            StartTutorial();
        }

        public void Skip()
        {
            PlayerPrefs.SetInt(PrefDone, 1);
            PlayerPrefs.Save();
            _current = -1;
            OnUpdated?.Invoke();
            Astrion.UI.ToastUI.Instance?.Show("튜토리얼을 건너뜁니다. (F1 으로 다시 보기)",
                new Color(0.78f, 0.72f, 0.55f));
        }

        // ─────────────────────── progression ───────────────────────

        private void Update()
        {
            if (!Active) return;
            // Skip detection while a text field has focus so typing doesn't
            // accidentally trigger steps.
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                var sel = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
                if (sel != null && sel.GetComponent<UnityEngine.UI.InputField>() != null) return;
            }
            try
            {
                if (_steps[_current].detect()) Advance();
            }
            catch { /* detection callbacks should never throw the loop */ }
        }

        private void Advance()
        {
            Astrion.Audio.SoundSystem.Instance?.Play(Astrion.Audio.Sfx.Blip);
            _current++;
            if (_current >= _steps.Count) Complete();
            else OnUpdated?.Invoke();
        }

        private void Complete()
        {
            PlayerPrefs.SetInt(PrefDone, 1);
            PlayerPrefs.Save();
            _current = -1;
            Astrion.Audio.SoundSystem.Instance?.Play(Astrion.Audio.Sfx.Levelup);
            InventorySystem.Instance?.Add("stardust", 50);
            Astrion.UI.ToastUI.Instance?.Show("★ 튜토리얼 완료!  +50 별 가루",
                new Color(0.95f, 0.82f, 0.35f));
            OnUpdated?.Invoke();
        }

        // ─────────────────────── detectors ───────────────────────

        private static bool DetectMove()
        {
            return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
                || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)
                || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)
                || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
        }
        private static bool DetectJump() => Input.GetKeyDown(KeyCode.Space);
        private static bool DetectAttack()
        {
            return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)
                || Input.GetMouseButtonDown(0);
        }
        private static bool DetectInventory() => Input.GetKeyDown(KeyCode.I);
        private static bool DetectPotion()
        {
            return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2);
        }
        private static bool DetectWorldmap() => Input.GetKeyDown(KeyCode.M);

        private static bool IsGameScene(string name)
        {
            return name != "LoginScene"
                && name != "CharacterSelectScene"
                && name != "CharacterCreateScene";
        }
    }
}
