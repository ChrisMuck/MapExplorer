using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>Time-of-day mood for the map style: sky, sun and ambient colors. Mood only; it never affects play.</summary>
    public static class MapStyleLighting
    {
        // t, sky rgb, sun rgb, sun intensity, ambient intensity, ground rgb
        private static readonly float[][] Keys =
        {
            new[] { 0f, 0.08f, 0.11f, 0.20f, 0.48f, 0.55f, 0.75f, 0.35f, 0.35f, 0.25f, 0.27f, 0.42f },
            new[] { 22f, 0.91f, 0.73f, 0.56f, 1.00f, 0.77f, 0.54f, 0.70f, 0.50f, 0.50f, 0.42f, 0.38f },
            new[] { 45f, 0.81f, 0.89f, 0.93f, 1.00f, 1.00f, 1.00f, 1.00f, 0.65f, 0.42f, 0.48f, 0.35f },
            new[] { 70f, 0.90f, 0.65f, 0.48f, 1.00f, 0.66f, 0.38f, 0.75f, 0.50f, 0.50f, 0.42f, 0.38f },
            new[] { 100f, 0.08f, 0.11f, 0.20f, 0.48f, 0.55f, 0.75f, 0.35f, 0.35f, 0.25f, 0.27f, 0.42f }
        };

        /// <summary>Applies the mood for <paramref name="value"/> (0 to 100) and returns daylight from 0 to 1.</summary>
        public static float Apply(Camera camera, Light sun, float value)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            var index = 0;
            while (index < Keys.Length - 2 && value > Keys[index + 1][0])
            {
                index++;
            }

            var a = Keys[index];
            var b = Keys[index + 1];
            var u = (value - a[0]) / (b[0] - a[0]);
            float L(int i) => Mathf.Lerp(a[i], b[i], u);
            camera.backgroundColor = new Color(L(1), L(2), L(3));
            sun.color = new Color(L(4), L(5), L(6));
            var sunIntensity = L(7);
            sun.intensity = sunIntensity * 0.9f;
            var ambient = L(8);
            var ground = new Color(L(9), L(10), L(11));
            RenderSettings.ambientSkyColor = Color.white * (ambient * 0.8f);
            RenderSettings.ambientEquatorColor = Color.Lerp(Color.white, ground, 0.5f) * (ambient * 0.65f);
            RenderSettings.ambientGroundColor = ground * (ambient * 0.8f);
            var angle = Mathf.PI * (0.08f + 0.84f * value / 100f);
            var direction = new Vector3(Mathf.Cos(angle) * 40f, Mathf.Sin(angle) * 34f + 5f, 16f);
            sun.transform.rotation = Quaternion.LookRotation(-direction.normalized);
            return Mathf.Clamp01((sunIntensity - 0.35f) / 0.65f);
        }
    }
}
