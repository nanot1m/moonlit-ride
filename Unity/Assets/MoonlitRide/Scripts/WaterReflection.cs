using UnityEngine;
namespace MoonlitRide
{
    // Built-in pipeline planar reflection, shared by gameplay and screenshot cameras.
    public sealed class WaterReflection : MonoBehaviour
    {
        Camera reflection; RenderTexture texture; Material material; static bool rendering;
        public static Matrix4x4 Mirror(float height) { var m = Matrix4x4.identity; m.m11 = -1; m.m13 = 2 * height; return m; }
        void Awake()
        {
            gameObject.layer = 4; material = GetComponent<Renderer>().sharedMaterial;
            reflection = new GameObject("Sea reflection", typeof(Camera)).GetComponent<Camera>(); reflection.enabled = false;
        }
        void OnWillRenderObject()
        {
            var source = Camera.current; if (!source || rendering || source == reflection) return;
            rendering = true; bool previousInvert = GL.invertCulling;
            try
            {
                int width = Mathf.Clamp(source.pixelWidth / 2, 384, 1024);
                int heightPixels = Mathf.Clamp(Mathf.RoundToInt(width / source.aspect), 256, 1024);
                if (!texture || texture.width != width || texture.height != heightPixels) {
                    if (texture) { texture.Release(); Destroy(texture); }
                    var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
                    texture = new RenderTexture(width, heightPixels, 24, format) { name = "Sea reflection", wrapMode = TextureWrapMode.Clamp, useMipMap = true, autoGenerateMips = true, filterMode = FilterMode.Trilinear };
                    material.SetTexture("_ReflectionTex", texture);
                }
                reflection.CopyFrom(source); reflection.enabled = false; reflection.targetTexture = texture;
                reflection.cullingMask = source.cullingMask & ~(1 << 4); reflection.useOcclusionCulling = false;
                float height = transform.position.y + .05f;
                var matrix = Mirror(height);
                reflection.transform.position = matrix.MultiplyPoint(source.transform.position);
                reflection.transform.rotation = Quaternion.LookRotation(matrix.MultiplyVector(source.transform.forward), matrix.MultiplyVector(source.transform.up));
                reflection.worldToCameraMatrix = source.worldToCameraMatrix * matrix;
                var point = reflection.worldToCameraMatrix.MultiplyPoint(new Vector3(0, height + .025f, 0));
                var normal = reflection.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
                reflection.projectionMatrix = reflection.CalculateObliqueMatrix(new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(point, normal)));
                material.SetMatrix("_ReflectionVP", GL.GetGPUProjectionMatrix(reflection.projectionMatrix, true) * reflection.worldToCameraMatrix);
                GL.invertCulling = !previousInvert; reflection.Render();
            }
            finally { GL.invertCulling = previousInvert; rendering = false; }
        }
        void OnDestroy() { if (reflection) Destroy(reflection.gameObject); if (texture) { texture.Release(); Destroy(texture); } }
    }
}
