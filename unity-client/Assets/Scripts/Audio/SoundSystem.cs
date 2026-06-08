using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astrion.Audio
{
    public enum Sfx { Blip, Hit, Hurt, Die, Pickup, Levelup, Portal }
    public enum Bgm { None, Menu, City, Field }

    /// Master audio runtime. Two AudioSources — one for SFX (one-shot pool)
    /// and one for BGM (looping). Volumes persist via PlayerPrefs so the
    /// player's choice survives restarts.
    ///
    /// DDOL singleton on networkGo, built on the login scene boot path with
    /// the other persistent systems. Scene-keyed BGM auto-switches via
    /// SceneManager.activeSceneChanged.
    public class SoundSystem : MonoBehaviour
    {
        public static SoundSystem Instance { get; private set; }

        // PlayerPrefs keys for the three volume sliders.
        private const string PrefMaster = "audio.master";
        private const string PrefSfx    = "audio.sfx";
        private const string PrefBgm    = "audio.bgm";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(PrefMaster, 0.80f);
            set { PlayerPrefs.SetFloat(PrefMaster, Mathf.Clamp01(value)); PlayerPrefs.Save(); Instance?.ApplyVolumes(); }
        }
        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(PrefSfx, 0.85f);
            set { PlayerPrefs.SetFloat(PrefSfx, Mathf.Clamp01(value)); PlayerPrefs.Save(); Instance?.ApplyVolumes(); }
        }
        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat(PrefBgm, 0.55f);
            set { PlayerPrefs.SetFloat(PrefBgm, Mathf.Clamp01(value)); PlayerPrefs.Save(); Instance?.ApplyVolumes(); }
        }

        private AudioSource _sfxSrc;
        private AudioSource _bgmSrc;
        private readonly Dictionary<Sfx, AudioClip> _sfxCache = new();
        private readonly Dictionary<Bgm, AudioClip> _bgmCache = new();
        private Bgm _currentBgm = Bgm.None;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sfxSrc = gameObject.AddComponent<AudioSource>();
            _sfxSrc.playOnAwake = false;
            _sfxSrc.spatialBlend = 0f;

            _bgmSrc = gameObject.AddComponent<AudioSource>();
            _bgmSrc.playOnAwake = false;
            _bgmSrc.loop = true;
            _bgmSrc.spatialBlend = 0f;

            ApplyVolumes();
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // Subscribe to PlayerStats events for the level-up + hurt cues
            // here rather than from PlayerStats itself, keeping the audio
            // dependency one-way (audio knows about gameplay, not vice
            // versa).
            HookPlayerStats();
            // Kick off the right BGM for the current scene on first frame.
            UpdateBgmForScene(SceneManager.GetActiveScene().name);
        }

        private void HookPlayerStats()
        {
            if (Astrion.Game.PlayerStats.Instance != null)
            {
                Astrion.Game.PlayerStats.Instance.OnLeveledUp += () => Play(Sfx.Levelup);
            }
            else
            {
                // Stats singleton spawns after us in some scenes — retry next frame.
                StartCoroutine(RetryHook());
            }
        }
        private System.Collections.IEnumerator RetryHook()
        {
            float waited = 0f;
            while (waited < 5f && Astrion.Game.PlayerStats.Instance == null)
            {
                yield return null;
                waited += Time.unscaledDeltaTime;
            }
            HookPlayerStats();
        }

        // ──────────────────────── PUBLIC API ────────────────────────

        public void Play(Sfx kind)
        {
            var clip = GetSfx(kind);
            if (clip == null || _sfxSrc == null) return;
            _sfxSrc.PlayOneShot(clip, 1f);
        }

        public void PlayBgm(Bgm kind)
        {
            if (kind == _currentBgm) return;
            _currentBgm = kind;
            if (kind == Bgm.None)
            {
                _bgmSrc.Stop();
                _bgmSrc.clip = null;
                return;
            }
            var clip = GetBgm(kind);
            if (clip == null || _bgmSrc == null) return;
            _bgmSrc.clip = clip;
            _bgmSrc.Play();
        }

        // ──────────────────── catalog + volume ────────────────────

        private AudioClip GetSfx(Sfx kind)
        {
            if (_sfxCache.TryGetValue(kind, out var c) && c != null) return c;
            AudioClip built = kind switch
            {
                Sfx.Blip    => SfxBuilder.Blip(),
                Sfx.Hit     => SfxBuilder.Hit(),
                Sfx.Hurt    => SfxBuilder.Hurt(),
                Sfx.Die     => SfxBuilder.Die(),
                Sfx.Pickup  => SfxBuilder.Pickup(),
                Sfx.Levelup => SfxBuilder.Levelup(),
                Sfx.Portal  => SfxBuilder.Portal(),
                _ => null,
            };
            if (built != null) _sfxCache[kind] = built;
            return built;
        }

        private AudioClip GetBgm(Bgm kind)
        {
            if (_bgmCache.TryGetValue(kind, out var c) && c != null) return c;
            AudioClip built = kind switch
            {
                Bgm.Menu  => SfxBuilder.MenuLoop(),
                Bgm.City  => SfxBuilder.CityLoop(),
                Bgm.Field => SfxBuilder.FieldLoop(),
                _ => null,
            };
            if (built != null) _bgmCache[kind] = built;
            return built;
        }

        private void ApplyVolumes()
        {
            if (_sfxSrc != null) _sfxSrc.volume = MasterVolume * SfxVolume;
            if (_bgmSrc != null) _bgmSrc.volume = MasterVolume * BgmVolume;
        }

        // ──────────────────── scene → BGM mapping ────────────────────

        private void OnSceneChanged(Scene prev, Scene next)
        {
            UpdateBgmForScene(next.name);
        }

        private void UpdateBgmForScene(string sceneName)
        {
            Bgm target = sceneName switch
            {
                "LoginScene" or "CharacterSelectScene" or "CharacterCreateScene" => Bgm.Menu,
                "SolariaScene" or "PyresummitScene" or "VerdaglenScene"
                    or "NightportScene" or "TidehavenScene" or "MainScene" => Bgm.City,
                _ => Bgm.Field,
            };
            PlayBgm(target);
        }
    }
}
