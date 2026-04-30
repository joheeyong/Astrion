using UnityEngine;

namespace Astrion.UI
{
    public class LoginBackground : MonoBehaviour
    {
        private void Awake()
        {
            CreateFireflies();
        }

        private Texture2D CreateGlowTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    float alpha = Mathf.Clamp01(1f - dist * dist);
                    alpha *= alpha; // softer falloff
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private void CreateFireflies()
        {
            var go = new GameObject("Fireflies");
            var ps = go.AddComponent<ParticleSystem>();
            go.transform.position = new Vector3(0, 0, 50);

            var main = ps.main;
            main.maxParticles = 80;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.02f;

            var emission = ps.emission;
            emission.rateOverTime = 8;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(120, 70, 3);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.12f;
            noise.octaveCount = 2;

            // Twinkle effect
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.15f),
                    new GradientAlphaKey(0.2f, 0.4f),
                    new GradientAlphaKey(1f, 0.6f),
                    new GradientAlphaKey(0.4f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0f);
            sizeCurve.AddKey(0.1f, 0.8f);
            sizeCurve.AddKey(0.3f, 1f);
            sizeCurve.AddKey(0.5f, 0.5f);
            sizeCurve.AddKey(0.7f, 1f);
            sizeCurve.AddKey(0.9f, 0.6f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Glow texture + material
            var glowTex = CreateGlowTexture(64);
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = glowTex;
            mat.color = Color.white;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
        }
    }
}
