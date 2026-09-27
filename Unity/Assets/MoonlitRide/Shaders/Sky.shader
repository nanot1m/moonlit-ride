Shader "MoonlitRide/Sky" {
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; };
 v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o; }
 float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
 float noise(float2 p) { float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y); }
 fixed4 frag(v2f i):SV_Target {
 float3 d=normalize(i.direction);
 float3 col=lerp(float3(.018,.06,.30),float3(.008,.022,.16),smoothstep(-.08,.5,d.y));
 float2 p=float2(d.x*6+d.z*2,d.y*24);
 float cloud=noise(p)*.55+noise(p*2.1)*.3+noise(p*4.3)*.15;
 col+=float3(.065,.085,.15)*smoothstep(.40,.67,cloud)*smoothstep(.01,.18,d.y)*(1-smoothstep(.25,.6,d.y));
 // Match the distance-fog colour at the horizon, including sea reflections.
 col=lerp(col,unity_FogColor.rgb,1-smoothstep(.015,.24,abs(d.y)));
 return float4(col,1);
 }
 ENDCG
 } }
}
