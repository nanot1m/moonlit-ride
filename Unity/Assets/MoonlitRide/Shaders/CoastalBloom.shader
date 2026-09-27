Shader "MoonlitRide/CoastalBloom" {
 Properties {_MainTex("Source",2D)="white"{} _Bloom("Bloom",2D)="black"{}}
 SubShader {Cull Off ZWrite Off ZTest Always
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex,_Bloom,_CameraDepthNormalsTexture;float2 _Direction;float4 _MainTex_TexelSize;
 float4 threshold(v2f_img i):SV_Target {float3 c=tex2D(_MainTex,i.uv).rgb;float peak=max(c.r,max(c.g,c.b));return float4(c*saturate((peak-.65)/max(.001,peak)),1);}
 float4 blur(v2f_img i):SV_Target {float3 c=tex2D(_MainTex,i.uv).rgb*.227027; c+=(tex2D(_MainTex,i.uv+_Direction*1.384615).rgb+tex2D(_MainTex,i.uv-_Direction*1.384615).rgb)*.316216; c+=(tex2D(_MainTex,i.uv+_Direction*3.230769).rgb+tex2D(_MainTex,i.uv-_Direction*3.230769).rgb)*.070270;return float4(c,1);}
 float ink(float2 uv,float2 shift,float depth,float3 normal) {
   float d;float3 n;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,uv+shift),d,n);
   float silhouette=smoothstep(.002,.008,abs(d-depth)/max(depth,.015));
   float crease=smoothstep(.42,.68,1-dot(normal,n))*.45;
   return max(silhouette,crease)*step(depth,.995);
 }
 float4 composite(v2f_img i):SV_Target {
   float3 c=tex2D(_MainTex,i.uv).rgb+tex2D(_Bloom,i.uv).rgb*.24;
   c=1-exp(-c*1.3);float l=dot(c,float3(.2126,.7152,.0722));c=max(0,lerp(l.xxx,c,1.04));
   float2 uv=i.uv;
   #if UNITY_UV_STARTS_AT_TOP
   if(_MainTex_TexelSize.y<0)uv.y=1-uv.y;
   #endif
   float d;float3 n;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,uv),d,n);
   float2 pixel=abs(_MainTex_TexelSize.xy)*.8;
   float edge=max(max(ink(uv,float2(pixel.x,0),d,n),ink(uv,float2(-pixel.x,0),d,n)),max(ink(uv,float2(0,pixel.y),d,n),ink(uv,float2(0,-pixel.y),d,n)));
   edge*=1-smoothstep(18,105,d*_ProjectionParams.z);
   c=lerp(c,c*float3(.38,.43,.57),edge*.48);
   return float4(c,1);
 }
 ENDCG
 Pass {CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment threshold
 ENDCG}
 Pass {CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment blur
 ENDCG}
 Pass {CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment composite
 ENDCG}
 }
}
