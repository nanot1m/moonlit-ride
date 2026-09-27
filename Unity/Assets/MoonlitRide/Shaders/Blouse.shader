Shader "MoonlitRide/Blouse" {
 Properties {_MainTex("Pattern coordinates",2D)="white"{}}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 sampler2D _MainTex;
 struct Input {float2 uv_MainTex;};
 void surf(Input i,inout SurfaceOutputStandard o) {
 float2 p=i.uv_MainTex*13, cell=floor(p), q=frac(p)-.5;
 float random=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
 float fleck=(1-smoothstep(.07,.12,length(q)))*step(.55,random);
 float stitch=(1-smoothstep(.009,.023,min(abs(q.x),abs(q.y))))*(1-smoothstep(.055,.12,length(q)))*step(.94,random);
 o.Albedo=lerp(float3(.008,.010,.018),float3(.92,.94,.98),saturate(fleck+stitch));
 o.Smoothness=.22+fleck*.18;o.Metallic=fleck*.12;o.Alpha=1;
 }
 ENDCG
 } Fallback "Diffuse"
}
