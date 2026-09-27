Shader "MoonlitRide/Painted" {
 Properties { _Color("Paint",Color)=(1,1,1,1) _MainTex("Pigment",2D)="white"{} _Glossiness("Soft highlight",Range(0,1))=.2 _Metallic("Metal accent",Range(0,1))=0 _EmissionColor("Glow",Color)=(0,0,0,0) }
 SubShader { Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Painted fullforwardshadows addshadow
 #pragma target 3.0
 #include "PaintedLighting.cginc"
 sampler2D _MainTex; fixed4 _Color,_EmissionColor; half _Glossiness;
 struct Input {float2 uv_MainTex;};
 void surf(Input i,inout SurfaceOutput o) {
   o.Albedo=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb;
   o.Gloss=_Glossiness; o.Alpha=1;
   o.Emission=o.Albedo*float3(.022,.028,.045)+_EmissionColor.rgb*.22;
 }
 ENDCG
 } Fallback "Diffuse"
}
