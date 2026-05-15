using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    public class ToastUI : MonoBehaviour
    {
        public static ToastUI Instance { get; private set; }

        [SerializeField] private Transform stackRoot;
        [SerializeField] private GameObject toastTemplate;
        [SerializeField] private float lifetime = 3.0f;
        [SerializeField] private float fadeTime = 0.5f;
        [SerializeField] private int maxActive = 5;

        private class ActiveToast
        {
            public RectTransform rt;
            public CanvasGroup cg;
            public Text label;
            public float bornAt;
            public bool inUse;
        }

        private readonly List<ActiveToast> _pool = new List<ActiveToast>();

        private void Awake()
        {
            if (Instance != null && Instance != this) return;
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(string text, Color color)
        {
            if (stackRoot == null || toastTemplate == null) return;

            // Find a free slot, or recycle the oldest
            ActiveToast slot = null;
            foreach (var t in _pool)
            {
                if (!t.inUse) { slot = t; break; }
            }
            if (slot == null && _pool.Count < maxActive)
            {
                slot = CreateSlot();
                _pool.Add(slot);
            }
            if (slot == null)
            {
                // recycle oldest
                float oldestT = float.MaxValue;
                foreach (var t in _pool)
                    if (t.bornAt < oldestT) { oldestT = t.bornAt; slot = t; }
            }
            if (slot == null) return;

            slot.label.text = text;
            slot.label.color = color;
            slot.cg.alpha = 1f;
            slot.rt.gameObject.SetActive(true);
            slot.bornAt = Time.unscaledTime;
            slot.inUse = true;

            ReflowStack();
        }

        private ActiveToast CreateSlot()
        {
            var go = Instantiate(toastTemplate, stackRoot);
            go.name = $"Toast_{_pool.Count}";
            var rt = go.GetComponent<RectTransform>();
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            var label = go.GetComponentInChildren<Text>();
            return new ActiveToast { rt = rt, cg = cg, label = label };
        }

        private void ReflowStack()
        {
            // Active slots in order of bornAt descending (newest on top)
            var active = new List<ActiveToast>();
            foreach (var t in _pool) if (t.inUse) active.Add(t);
            active.Sort((a, b) => b.bornAt.CompareTo(a.bornAt));

            for (int i = 0; i < active.Count; i++)
            {
                var t = active[i];
                t.rt.anchorMin = new Vector2(1, 1);
                t.rt.anchorMax = new Vector2(1, 1);
                t.rt.pivot = new Vector2(1, 1);
                t.rt.anchoredPosition = new Vector2(-12, -200 - i * 36);
            }
        }

        private void Update()
        {
            bool changed = false;
            float now = Time.unscaledTime;
            foreach (var t in _pool)
            {
                if (!t.inUse) continue;
                float age = now - t.bornAt;
                if (age >= lifetime + fadeTime)
                {
                    t.inUse = false;
                    t.rt.gameObject.SetActive(false);
                    changed = true;
                }
                else if (age >= lifetime)
                {
                    t.cg.alpha = Mathf.Clamp01(1f - (age - lifetime) / fadeTime);
                }
            }
            if (changed) ReflowStack();
        }
    }
}
