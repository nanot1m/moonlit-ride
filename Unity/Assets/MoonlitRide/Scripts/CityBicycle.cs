using System.Collections.Generic;
using UnityEngine;
namespace MoonlitRide {
    public sealed class CityBicycle : MonoBehaviour {
        readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        Transform steering,crank; readonly Transform[] wheels=new Transform[2],pedals=new Transform[2];
        static readonly Vector3 SteeringPivot=new Vector3(0,1.3f,-.64f);
        Transform Part(string asset,Transform parent,Vector3 point) {
            var pivot=new GameObject(asset+" pivot").transform;pivot.SetParent(parent,false);pivot.localPosition=point;
            var source=Resources.Load<GameObject>("Bicycle/"+asset);
            if(!source)throw new System.Exception("Missing bicycle asset "+asset);
            var model=Instantiate(source,pivot,false);
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>()) {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++) {
                    string key=slots[i].name;
                    if(!materials.TryGetValue(key,out var mat)) {
                        mat=new Material(Shader.Find("MoonlitRide/Painted")){name=key,color=slots[i].color};
                        bool metal=key.Contains("chrome") || key.Contains("steel") || key.Contains("enamel");
                        mat.SetFloat("_Metallic",metal?.65f:0);mat.SetFloat("_Glossiness",metal?.55f:.22f);materials.Add(key,mat);
                    }
                    slots[i]=mat;
                }
                renderer.sharedMaterials=slots;
            }
            return pivot;
        }
        public void Initialize() {
            Part("Frame",transform,Vector3.zero);
            steering=Part("Steering",transform,SteeringPivot);
            wheels[0]=Part("Wheel",steering,new Vector3(0,.56f,-.83f)-SteeringPivot);
            wheels[1]=Part("Wheel",transform,new Vector3(0,.56f,.83f));
            crank=Part("Crank",transform,new Vector3(0,.54f,.1f));
            for(int i=0;i<2;i++)pedals[i]=Part("Pedal",transform,Vector3.zero);
        }
        public void Pose(RideState state) {
            steering.localRotation=Quaternion.Euler(0,state.Steering*Mathf.Rad2Deg,0);
            foreach(var wheel in wheels)wheel.localRotation=Quaternion.Euler(-state.WheelAngle*Mathf.Rad2Deg,0,0);
            crank.localRotation=Quaternion.Euler(-state.Cadence*Mathf.Rad2Deg,0,0);
            for(int i=0;i<2;i++)pedals[i].localPosition=Foot(i,state.Cadence)-Vector3.up*.05f;
        }
        public Vector3 Hand(float side) => SteeringPivot+steering.localRotation*(new Vector3(side*.39f,1.80f,-.57f)-SteeringPivot);
        public static Vector3 Foot(int side,float cadence) {
            float angle=cadence+side*Mathf.PI;
            return new Vector3(side==0?-.23f:.23f,.59f+Mathf.Sin(angle)*.21f,.1f+Mathf.Cos(angle)*.21f);
        }
        void OnDestroy(){foreach(var mat in materials.Values)Destroy(mat);}
    }
}
