using System.Collections.Generic;
using UnityEngine;
namespace MoonlitRide {
    // Quaternius CC0 meshes and textures, shared by all streamed chunks.
    public sealed class NatureAssets {
        readonly Dictionary<string,GameObject> models=new Dictionary<string,GameObject>();
        readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        public Transform Place(Transform parent,string name,Vector3 position,float scale,float yaw) {
            if(!models.TryGetValue(name,out var model)) {model=Resources.Load<GameObject>("Nature/"+name);models.Add(name,model);}
            if(!model) throw new System.Exception("Missing nature asset: "+name);
            var placement=new GameObject(name).transform;placement.SetParent(parent,false);
            var instance=Object.Instantiate(model,placement,false);instance.name=name;
            placement.localPosition=position;placement.localRotation=Quaternion.Euler(0,yaw,0);placement.localScale=Vector3.one*scale;
            foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>()) {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++) {
                    string key=slots[i].name.Replace(" (Instance)","");
                    if(!materials.TryGetValue(key,out var mat)) {
                        bool foliage=key.Contains("Leaves") || key=="Grass";
                        mat=new Material(Shader.Find(foliage?"MoonlitRide/Foliage":"MoonlitRide/Painted"));mat.name=key;
                        mat.mainTexture=Resources.Load<Texture2D>("Nature/"+(key.StartsWith("Rock")?"Rocks":key));
                        if(!mat.mainTexture) throw new System.Exception("Missing nature texture: "+key);
                        if(!foliage)mat.SetFloat("_Glossiness",.14f);
                        materials.Add(key,mat);
                    }
                    slots[i]=mat;
                }
                renderer.sharedMaterials=slots;
            }
            return placement;
        }
        public void Dispose(){foreach(var mat in materials.Values)Object.Destroy(mat);}
    }
}
