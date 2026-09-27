using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonlitRide
{
    public static class Geometry
    {
        public static Material Material(string hex, float emission = 0)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            if (emission > 0) return new Material(Shader.Find("MoonlitRide/Glow")) { color = color * Mathf.Min(emission, 1.5f) };
            var m = new Material(Shader.Find("Standard")) { color = color };
            m.SetFloat("_Glossiness", .22f);
            return m;
        }
        public static Transform Shape(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var g = GameObject.CreatePrimitive(type); g.transform.SetParent(parent, false);
            g.transform.localPosition = position; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(g.GetComponent<Collider>());
            return g.transform;
        }
        public static Transform Box(Transform p, Vector3 pos, Vector3 size, Material m) => Shape(PrimitiveType.Cube, p, pos, size, m);
        public static Transform Ball(Transform p, Vector3 pos, Vector3 size, Material m) => Shape(PrimitiveType.Sphere, p, pos, size, m);
        public static Transform Rod(Transform p, Vector3 a, Vector3 b, float radius, Material m)
        {
            var t = Shape(PrimitiveType.Cylinder, p, Vector3.zero, Vector3.one, m); PoseRod(t, a, b, radius); return t;
        }
        public static void PoseRod(Transform t, Vector3 a, Vector3 b, float radius)
        {
            t.localPosition = (a + b) / 2; t.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            t.localScale = new Vector3(radius * 2, (b - a).magnitude / 2, radius * 2);
        }
        public static Transform MeshObject(string name, Transform p, Mesh mesh, Material m)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(p, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<Renderer>().sharedMaterial = m; return go.transform;
        }
        public static Mesh Mesh(Vector3[] points, int[] indices, Vector2[] uv = null)
        {
            var m = new Mesh(); m.vertices = points; m.triangles = indices;
            if (uv != null) m.uv = uv; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        public static Mesh Torus(float radius, float tube, int segments = 32)
        {
            var v = new Vector3[segments * 8]; var ix = new List<int>();
            for (int i = 0; i < segments; i++) for (int j = 0; j < 8; j++)
            {
                float a = i * Mathf.PI * 2 / segments, b = j * Mathf.PI / 4;
                v[i * 8 + j] = new Vector3(Mathf.Sin(b) * tube, Mathf.Sin(a) * (radius + Mathf.Cos(b) * tube), Mathf.Cos(a) * (radius + Mathf.Cos(b) * tube));
                int n = ((i + 1) % segments) * 8, k = (j + 1) % 8, c = i * 8;
                ix.AddRange(new[] { c + j, n + j, c + k, c + k, n + j, n + k });
            }
            return Mesh(v, ix.ToArray());
        }
        public static Mesh Cone(float radius, float height, int sides)
        {
            var v = new List<Vector3>(); var ix = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                int n = v.Count;
                v.Add(new Vector3(Mathf.Sin(a) * radius, 0, Mathf.Cos(a) * radius));
                v.Add(Vector3.up * height); v.Add(new Vector3(Mathf.Sin(b) * radius, 0, Mathf.Cos(b) * radius));
                ix.AddRange(new[] { n, n + 1, n + 2 });
            }
            return Mesh(v.ToArray(), ix.ToArray());
        }
        // Combine static scenery per material, keeping streamed chunks cheap to draw.
        public static void ReflectX(Mesh mesh)
        {
            var vertices = mesh.vertices; var normals = mesh.normals; var indices = mesh.triangles; var tangents = mesh.tangents;
            for (int i = 0; i < vertices.Length; i++) { vertices[i].x = -vertices[i].x; normals[i].x = -normals[i].x; }
            for (int i = 0; i < indices.Length; i += 3) { int swap = indices[i + 1]; indices[i + 1] = indices[i + 2]; indices[i + 2] = swap; }
            mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = indices; mesh.RecalculateBounds();
            if (tangents.Length == vertices.Length) { for (int i = 0; i < tangents.Length; i++) { tangents[i].x = -tangents[i].x; tangents[i].w = -tangents[i].w; } mesh.tangents = tangents; }
        }
        public static void Combine(Transform root, bool reflectX = false)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var assigned = f.GetComponent<Renderer>().sharedMaterials;
                for(int sub=0;sub<f.sharedMesh.subMeshCount;sub++) {
                    var material=assigned[Mathf.Min(sub,assigned.Length-1)];
                    if (!groups.TryGetValue(material, out var list)) groups.Add(material, list = new List<CombineInstance>());
                    list.Add(new CombineInstance { mesh = f.sharedMesh, subMeshIndex=sub, transform = root.worldToLocalMatrix * f.transform.localToWorldMatrix });
                }
            }
            foreach (Transform child in root) { child.gameObject.SetActive(false); Object.Destroy(child.gameObject); }
            foreach (var group in groups)
            {
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 }; mesh.CombineMeshes(group.Value.ToArray());
                if (reflectX) ReflectX(mesh);
                MeshObject("Scenery", root, mesh, group.Key);
            }
        }
    }
}
