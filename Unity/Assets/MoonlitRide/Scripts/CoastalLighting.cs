using UnityEngine;
using UnityEngine.Rendering;
namespace MoonlitRide
{
    public sealed partial class CoastalWorld
    {
        Texture2D windowCookie;
        // Complete the fade before the nearest streamed chunk can be removed (72 m).
        public static float LightFade(float distance) => 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(28, 68, distance));
        void WindowLight(Transform scenery, float front, float ground, float z, float height)
        {
            if (!windowCookie) {
                const int size = 128;
                windowCookie = new Texture2D(size, size, TextureFormat.RGBA32, true, true) { name = "Soft window aperture and mullions", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
                    float u = Mathf.Abs((x + .5f) / size * 2 - 1), v = Mathf.Abs((y + .5f) / size * 2 - 1);
                    float edge = (1 - Mathf.SmoothStep(.57f, .67f, u)) * (1 - Mathf.SmoothStep(.78f, .88f, v));
                    float bars = Mathf.SmoothStep(.025f, .055f, u) * Mathf.SmoothStep(.025f, .055f, v);
                    pixels[y * size + x] = new Color(1, 1, 1, edge * bars);
                }
                windowCookie.SetPixels(pixels); windowCookie.Apply(true, true);
            }
            // Closed facades have no room interiors. The cookie models the aperture;
            // real foreground geometry also casts shadows into the spot map.
            var light = new GameObject("Window spill with mullion shadows", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(scenery.parent, false);
            light.transform.position = new Vector3(-front + .8f, ground + height, z);
            light.transform.rotation = Quaternion.LookRotation(new Vector3(1, -.6f, 0));
            light.type = LightType.Spot; light.spotAngle = 95; light.innerSpotAngle = 75;
            light.color = new Color(1, .68f, .32f); light.intensity = 4; light.range = 18;
            light.cookie = windowCookie; light.renderMode = LightRenderMode.ForcePixel;
            light.shadows = LightShadows.Soft; light.shadowResolution = LightShadowResolution.Medium;
            light.shadowBias = .025f; light.shadowNormalBias = .12f; light.shadowNearPlane = .08f;
        }
    }
}
