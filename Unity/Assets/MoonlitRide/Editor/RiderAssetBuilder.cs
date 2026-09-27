using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoonlitRide.Editor
{
    public static class RiderAssetBuilder
    {
        [Serializable] sealed class Record { public string name; public float[] positions, normals, uv, uv2; public int[] triangles, boneIndices; public float[] boneWeights; }
        [Serializable] sealed class Export { public Record[] meshes; }
        [MenuItem("Moonlit Ride/Import Blender rider assets")]
        public static void Import()
        {
            string source = Path.GetFullPath("../art/RiderMeshes.json");
            if (!File.Exists(source)) throw new FileNotFoundException("Run art/build_rider.py in Blender first", source);
            string root = "Assets/MoonlitRide/Resources/Rider/";
            foreach (var record in JsonUtility.FromJson<Export>(File.ReadAllText(source)).meshes)
            {
                var vertices = new Vector3[record.positions.Length / 3]; var normals = new Vector3[vertices.Length]; var uv = new Vector2[vertices.Length];
                for (int i = 0; i < vertices.Length; i++) { vertices[i] = new Vector3(record.positions[i * 3], record.positions[i * 3 + 1], record.positions[i * 3 + 2]); normals[i] = new Vector3(record.normals[i * 3], record.normals[i * 3 + 1], record.normals[i * 3 + 2]); uv[i] = new Vector2(record.uv[i * 2], record.uv[i * 2 + 1]); }
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + record.name + ".asset");
                bool fresh = !mesh; if (fresh) mesh = new Mesh { name = record.name }; else mesh.Clear();
                mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = record.triangles;
                if(record.uv2 != null && record.uv2.Length == vertices.Length*2) {var second=new Vector2[vertices.Length];for(int i=0;i<second.Length;i++)second[i]=new Vector2(record.uv2[i*2],record.uv2[i*2+1]);mesh.uv2=second;}
                if(record.name=="Dress")mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
                if (record.boneWeights != null && record.boneWeights.Length == vertices.Length * 4)
                {
                    var weights = new BoneWeight[vertices.Length];
                    for (int i = 0; i < weights.Length; i++) { int k = i * 4; weights[i] = new BoneWeight { boneIndex0=record.boneIndices[k],boneIndex1=record.boneIndices[k+1],boneIndex2=record.boneIndices[k+2],boneIndex3=record.boneIndices[k+3],weight0=record.boneWeights[k],weight1=record.boneWeights[k+1],weight2=record.boneWeights[k+2],weight3=record.boneWeights[k+3] }; }
                    mesh.boneWeights = weights;
                }
                if (fresh) AssetDatabase.CreateAsset(mesh, root + record.name + ".asset"); else EditorUtility.SetDirty(mesh);
            }
            foreach (string file in Directory.GetFiles(root, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(file);
                if (!importer) continue;
                importer.textureType = file.Contains("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 8; importer.maxTextureSize = 1024; importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }
    }
}
