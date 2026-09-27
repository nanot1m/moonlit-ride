using UnityEngine;
namespace MoonlitRide
{
    [RequireComponent(typeof(Camera))]
    public sealed class CoastalBloom : MonoBehaviour
    {
        Material material;
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!material) material=new Material(Shader.Find("MoonlitRide/CoastalBloom"));
            var a=RenderTexture.GetTemporary(Mathf.Max(1,source.width/4),Mathf.Max(1,source.height/4),0,RenderTextureFormat.ARGBHalf);
            var b=RenderTexture.GetTemporary(a.width,a.height,0,RenderTextureFormat.ARGBHalf);
            try {
                Graphics.Blit(source,a,material,0);
                for(int i=0;i<3;i++) {
                    material.SetVector("_Direction",new Vector4(1f/a.width,0,0,0));Graphics.Blit(a,b,material,1);
                    material.SetVector("_Direction",new Vector4(0,1f/a.height,0,0));Graphics.Blit(b,a,material,1);
                }
                material.SetTexture("_Bloom",a);Graphics.Blit(source,destination,material,2);
            }
            finally {RenderTexture.ReleaseTemporary(a);RenderTexture.ReleaseTemporary(b);}
        }
        void OnDestroy(){if(material)Destroy(material);}
    }
}
