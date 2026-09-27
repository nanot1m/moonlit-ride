using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoonlitRide
{
    public sealed class AnatomicalRider : MonoBehaviour
    {
        [Serializable] public sealed class Bone { public string name; public Vector3 start, end; }
        [Serializable] public sealed class Definition { public Bone[] bones; }
        Definition definition; Transform[] bones; readonly List<Mesh> meshes = new List<Mesh>();
        public void Initialize(Material garment, Material skin, Material leggings)
        {
            definition = JsonUtility.FromJson<Definition>(Resources.Load<TextAsset>("Rider/BodyRig").text);
            bones = new Transform[definition.bones.Length]; var binds = new Matrix4x4[bones.Length];
            for (int i=0;i<bones.Length;i++)
            {
                var rest=definition.bones[i]; bones[i]=new GameObject(rest.name).transform; bones[i].SetParent(transform,false);
                bones[i].localPosition=rest.start; bones[i].localRotation=Quaternion.FromToRotation(Vector3.up,rest.end-rest.start);
                binds[i]=Matrix4x4.TRS(rest.start,bones[i].localRotation,Vector3.one).inverse;
            }
            string[] names={"AnatomicalBodice","AnatomicalSkin","AnatomicalLeggings"}; Material[] materials={garment,skin,leggings};
            for(int i=0;i<names.Length;i++)
            {
                var mesh=Instantiate(Resources.Load<Mesh>("Rider/"+names[i])); meshes.Add(mesh); mesh.bindposes=binds;
                var renderer=new GameObject(names[i],typeof(SkinnedMeshRenderer)).GetComponent<SkinnedMeshRenderer>(); renderer.transform.SetParent(transform,false);
                renderer.sharedMesh=mesh;renderer.sharedMaterial=materials[i];renderer.bones=bones;renderer.rootBone=transform;renderer.updateWhenOffscreen=true;
                renderer.localBounds=new Bounds(new Vector3(0,1.5f,0),new Vector3(3,4,3));
            }
        }
        public void PoseTorso(Quaternion posture, Vector3 offset)
        {
            bones[0].localPosition=definition.bones[0].start+offset;
            bones[1].localPosition=definition.bones[1].start+offset;
            bones[1].localRotation=posture;
        }
        public void PoseLimb(int side,Vector3 shoulder,Vector3 elbow,Vector3 hand,Vector3 hip,Vector3 knee,Vector3 ankle)
        {
            int i=2+side*6; Segment(i,shoulder,elbow); Segment(i+1,elbow,hand);
            Segment(i+2,hand,hand+new Vector3(0,-.08f,-.10f).normalized * (definition.bones[i+2].end-definition.bones[i+2].start).magnitude);
            Segment(i+3,hip,knee);Segment(i+4,knee,ankle);
            Segment(i+5,ankle,ankle+new Vector3(0,-.035f,-.18f).normalized * (definition.bones[i+5].end-definition.bones[i+5].start).magnitude);
        }
        void Segment(int i,Vector3 a,Vector3 b)
        {
            bones[i].localPosition=a;bones[i].localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
            bones[i].localScale=new Vector3(1,(b-a).magnitude/(definition.bones[i].end-definition.bones[i].start).magnitude,1);
        }
        void OnDestroy(){foreach(var mesh in meshes)Destroy(mesh);}
    }
}
