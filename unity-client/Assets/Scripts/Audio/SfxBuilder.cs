using UnityEngine;

namespace Astrion.Audio
{
    /// Procedural sound-effect synth. Builds tiny PCM buffers in pure code
    /// so the project keeps zero audio assets. Each method returns a fresh
    /// AudioClip — call once and cache the result, the build is allocation-
    /// heavy. Retro-style (chiptune flavour) matches the pixel-art look.
    public static class SfxBuilder
    {
        // 22.05 kHz is half-CD-quality. Plenty for short retro effects and
        // halves memory vs 44.1 kHz; the 30 s music loops drop from ~5 MB
        // to ~2.5 MB at this rate.
        public const int SampleRate = 22050;

        // ────────────────────────── SFX ──────────────────────────

        /// Short rising 'blip' — UI selection / dialogue advance.
        public static AudioClip Blip()
        {
            return BuildClip("sfx_blip", 0.08f, (t, _) => {
                float freq = 800f + t * 1500f;
                float env  = Mathf.Exp(-t * 30f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.30f;
            });
        }

        /// Hit / impact — short noise burst plus low square thump.
        public static AudioClip Hit()
        {
            var rng = new System.Random(1);
            return BuildClip("sfx_hit", 0.10f, (t, _) => {
                float env   = Mathf.Exp(-t * 28f);
                float noise = (float)(rng.NextDouble() * 2 - 1);
                float thump = Mathf.Sin(2f * Mathf.PI * 180f * t);
                return (noise * 0.55f + thump * 0.45f) * env * 0.40f;
            });
        }

        /// Damage taken — descending sine + gritty noise tail.
        public static AudioClip Hurt()
        {
            var rng = new System.Random(7);
            return BuildClip("sfx_hurt", 0.18f, (t, _) => {
                float freq = 440f - t * 600f;
                if (freq < 80f) freq = 80f;
                float env  = Mathf.Exp(-t * 8f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float n    = (float)(rng.NextDouble() * 2 - 1) * 0.4f;
                return (tone + n * 0.5f) * env * 0.40f;
            });
        }

        /// Monster death — descending two-tone with a tail.
        public static AudioClip Die()
        {
            return BuildClip("sfx_die", 0.35f, (t, _) => {
                float freq = 520f - t * 800f;
                if (freq < 120f) freq = 120f;
                float env = Mathf.Exp(-t * 5.5f);
                float tone = Square(2f * Mathf.PI * freq * t);
                return tone * env * 0.30f;
            });
        }

        /// Pickup — rising arpeggio chime, three sine bursts.
        public static AudioClip Pickup()
        {
            float[] notes = { 880f, 1175f, 1567f }; // A5 D6 G6
            float per = 0.07f;
            float duration = per * notes.Length + 0.1f;
            return BuildClip("sfx_pickup", duration, (t, _) => {
                int idx = Mathf.Min((int)(t / per), notes.Length - 1);
                float nt = t - idx * per;
                float env = Mathf.Exp(-nt * 20f);
                return Mathf.Sin(2f * Mathf.PI * notes[idx] * t) * env * 0.32f;
            });
        }

        /// Level up — bright triumphant arpeggio.
        public static AudioClip Levelup()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.50f }; // C5 E5 G5 C6 E6
            float per = 0.10f;
            float duration = per * notes.Length + 0.45f;
            return BuildClip("sfx_levelup", duration, (t, _) => {
                int idx = Mathf.Min((int)(t / per), notes.Length - 1);
                float nt = t - idx * per;
                float env = Mathf.Exp(-nt * 4.5f);
                float fund = Mathf.Sin(2f * Mathf.PI * notes[idx] * t);
                float harm = Mathf.Sin(2f * Mathf.PI * notes[idx] * 2f * t) * 0.4f;
                return (fund + harm) * env * 0.28f;
            });
        }

        /// Portal — swirling sweep.
        public static AudioClip Portal()
        {
            return BuildClip("sfx_portal", 0.50f, (t, _) => {
                float lfo = Mathf.Sin(2f * Mathf.PI * 8f * t);
                float freq = 300f + lfo * 200f + t * 400f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.5f));
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                return tone * env * 0.30f;
            });
        }

        // ────────────────────────── BGM ──────────────────────────

        /// City BGM — slow, peaceful 8-note arpeggio with bass pedal.
        /// 32 s loop in C major.
        public static AudioClip CityLoop()
        {
            // C major arpeggio across two octaves
            float[] notes = { 261.63f, 329.63f, 392.00f, 523.25f,
                              392.00f, 329.63f, 261.63f, 196.00f };
            float per = 0.55f;
            float loopDur = per * notes.Length * 2f;
            return BuildMusicLoop("bgm_city", loopDur, (t, _) => {
                int idx = (int)((t / per) % notes.Length);
                float nt = (t - (long)(t / per) * per);
                float env = 0.4f * Mathf.Exp(-nt * 1.2f) + 0.15f;
                float bell = Mathf.Sin(2f * Mathf.PI * notes[idx] * t) * env;
                float bass = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.08f; // low C
                return (bell * 0.16f + bass) * 0.65f;
            });
        }

        /// Field BGM — moderate tempo, brighter, fifths progression.
        public static AudioClip FieldLoop()
        {
            // I-V-vi-IV in G major: G D Em C
            float[] roots = { 196.00f, 293.66f, 329.63f, 261.63f };
            float chordDur = 4.0f;
            float loopDur = chordDur * 4f;
            return BuildMusicLoop("bgm_field", loopDur, (t, _) => {
                int chordIdx = (int)((t / chordDur) % 4);
                float root = roots[chordIdx];
                float fifth = root * 1.5f;
                // Eighth-note arp triggers
                float beatT = (t % 0.5f);
                float beatEnv = Mathf.Exp(-beatT * 6f);
                float note = ((int)(t / 0.5f) % 2 == 0) ? root * 2f : fifth * 2f;
                float arp = Mathf.Sin(2f * Mathf.PI * note * t) * beatEnv * 0.18f;
                float pad = Mathf.Sin(2f * Mathf.PI * root * t) * 0.10f;
                return (arp + pad) * 0.6f;
            });
        }

        /// Menu BGM — slow ethereal pad, simple I-vi progression.
        public static AudioClip MenuLoop()
        {
            // A minor: Am F C G
            float[] roots = { 220.00f, 174.61f, 261.63f, 196.00f };
            float chordDur = 5.0f;
            float loopDur = chordDur * 4f;
            return BuildMusicLoop("bgm_menu", loopDur, (t, _) => {
                int chordIdx = (int)((t / chordDur) % 4);
                float root = roots[chordIdx];
                float third = root * 1.26f;
                float fifth = root * 1.5f;
                float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                float pad1 = Mathf.Sin(2f * Mathf.PI * root * t) * 0.12f;
                float pad2 = Mathf.Sin(2f * Mathf.PI * third * t) * 0.08f * lfo;
                float pad3 = Mathf.Sin(2f * Mathf.PI * fifth * t) * 0.08f * (1f - lfo);
                return (pad1 + pad2 + pad3) * 0.55f;
            });
        }

        // ────────────────────────── helpers ──────────────────────────

        private static AudioClip BuildClip(string name, float duration, System.Func<float, int, float> sampleFn)
        {
            int samples = Mathf.Max(1, (int)(SampleRate * duration));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(sampleFn(t, i), -1f, 1f);
            }
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// Same as BuildClip but applies a short cross-fade at the loop
        /// seam (first/last 0.05 s) so the boundary doesn't 'click' when
        /// the audio source loops.
        private static AudioClip BuildMusicLoop(string name, float duration, System.Func<float, int, float> sampleFn)
        {
            int samples = Mathf.Max(1, (int)(SampleRate * duration));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(sampleFn(t, i), -1f, 1f);
            }
            int fade = Mathf.Min(SampleRate / 20, samples / 4); // 50 ms or 25% of clip
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[samples - 1 - i] *= k;
            }
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Square(float phase)
        {
            return Mathf.Sin(phase) >= 0f ? 1f : -1f;
        }
    }
}
