Shader "MoonlitRide/Foliage" {
 Properties {_MainTex("Leaves",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Cutoff("Cutout",Range(0,1))=.4}
 SubShader {Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off
 CGPROGRAM
 #pragma surface surf Painted alphatest:_Cutoff vertex:vert addshadow fullforwardshadows
 #pragma target 3.0
 #include "PaintedLighting.cginc"
 sampler2D _MainTex;fixed4 _Color;
 struct Input {float2 uv_MainTex;};
 void vert(inout appdata_full v) {
 float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;
 float sway=sin(w.x*.8+w.z*.5+_Time.y*1.5)*.045+sin(w.z*2.1+_Time.y*2.8)*.012;
 v.vertex.xyz+=mul((float3x3)unity_WorldToObject,float3(sway,0,sway*.4));
 }
 void surf(Input i,inout SurfaceOutput o) {
 fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;float luminance=dot(c.rgb,float3(.2126,.7152,.0722));
 o.Albedo=lerp(luminance.xxx,c.rgb,.82)*float3(.92,.98,1.02);o.Alpha=c.a;o.Gloss=.04;o.Emission=o.Albedo*float3(.025,.035,.055);
 }
 ENDCG
 } Fallback "Transparent/Cutout/Diffuse"
}
