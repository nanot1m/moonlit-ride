Shader "MoonlitRide/CoastalBloom" {
 Properties {_MainTex("Source",2D)="white"{} _Bloom("Bloom",2D)="black"{}}
 SubShader {Cull Off ZWrite Off ZTest Always
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex,_Bloom;float2 _Direction;
 float4 threshold(v2f_img i):SV_Target {float3 c=tex2D(_MainTex,i.uv).rgb;float peak=max(c.r,max(c.g,c.b));return float4(c*saturate((peak-.65)/max(.001,peak)),1);}
 float4 blur(v2f_img i):SV_Target {float3 c=tex2D(_MainTex,i.uv).rgb*.227027; c+=(tex2D(_MainTex,i.uv+_Direction*1.384615).rgb+tex2D(_MainTex,i.uv-_Direction*1.384615).rgb)*.316216; c+=(tex2D(_MainTex,i.uv+_Direction*3.230769).rgb+tex2D(_MainTex,i.uv-_Direction*3.230769).rgb)*.070270;return float4(c,1);}
 float4 composite(v2f_img i):SV_Target {float3 c=tex2D(_MainTex,i.uv).rgb+tex2D(_Bloom,i.uv).rgb*.38;c=1-exp(-c*1.35);float l=dot(c,float3(.2126,.7152,.0722));return float4(max(0,lerp(l.xxx,c,1.15)),1);}
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
