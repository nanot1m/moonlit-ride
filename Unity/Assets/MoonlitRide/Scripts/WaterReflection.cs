using UnityEngine;
namespace MoonlitRide
{
    // Built-in pipeline planar reflection, shared by gameplay and screenshot cameras.
    public sealed class WaterReflection : MonoBehaviour
    {
        Camera reflection; RenderTexture texture; Material material; static bool rendering;
        void Awake()
        {
            gameObject.layer = 4; material = GetComponent<Renderer>().sharedMaterial;
            reflection = new GameObject("Sea reflection", typeof(Camera)).GetComponent<Camera>(); reflection.enabled = false;
            texture = new RenderTexture(768, 512, 16, RenderTextureFormat.ARGBHalf); texture.name = "Sea reflection"; texture.wrapMode = TextureWrapMode.Clamp; texture.useMipMap = true; texture.autoGenerateMips = true; texture.filterMode = FilterMode.Trilinear;
            material.SetTexture("_ReflectionTex", texture);
        }
        void OnWillRenderObject()
        {
            var source = Camera.current; if (!source || rendering || source == reflection) return;
            rendering = true; bool previousInvert = GL.invertCulling;
            try
            {
                reflection.CopyFrom(source); reflection.enabled = false; reflection.targetTexture = texture;
                reflection.cullingMask = source.cullingMask & ~(1 << 4); reflection.useOcclusionCulling = false;
                float height = transform.position.y + .05f;
                var matrix = Matrix4x4.identity; matrix.m11 = -1; matrix.m13 = 2 * height;
                reflection.worldToCameraMatrix = source.worldToCameraMatrix * matrix;
                reflection.transform.position = matrix.MultiplyPoint(source.transform.position);
                var point = reflection.worldToCameraMatrix.MultiplyPoint(new Vector3(0, height + .025f, 0));
                var normal = reflection.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
                reflection.projectionMatrix = source.CalculateObliqueMatrix(new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(point, normal)));
                material.SetMatrix("_ReflectionVP", GL.GetGPUProjectionMatrix(reflection.projectionMatrix, true) * reflection.worldToCameraMatrix);
                GL.invertCulling = !previousInvert; reflection.Render();
            }
            finally { GL.invertCulling = previousInvert; rendering = false; }
        }
        void OnDestroy() { if (reflection) Destroy(reflection.gameObject); if (texture) { texture.Release(); Destroy(texture); } }
    }
}
