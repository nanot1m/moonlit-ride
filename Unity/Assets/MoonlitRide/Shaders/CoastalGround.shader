Shader "MoonlitRide/CoastalGround" {
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Painted fullforwardshadows
 #pragma target 3.0
 #include "PaintedLighting.cginc"
 struct Input {float3 worldPos;float3 worldNormal;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 void surf(Input i,inout SurfaceOutput o) {
 float n=noise(i.worldPos.xz*.35)*.6+noise(i.worldPos.xz*2.5)*.4;
 float steep=1-smoothstep(.48,.82,i.worldNormal.y);
 float3 grass=lerp(float3(.09,.16,.07),float3(.24,.29,.13),n);
 float3 stone=lerp(float3(.24,.23,.20),float3(.46,.43,.34),n);
 float shore=1-smoothstep(-.5,2.5,i.worldPos.y);
 o.Albedo=lerp(lerp(grass,stone,steep),float3(.43,.39,.29)*(n*.4+.8),shore);
 o.Gloss=lerp(.1,.35,shore);o.Alpha=1;
 }
 ENDCG
 }Fallback "Diffuse"
}
