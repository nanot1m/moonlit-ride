Shader "MoonlitRide/Water" {
 Properties { _RideTime ("Ride time", Float) = 0 _ReflectionTex ("Planar reflection", 2D) = "black" {} }
 SubShader { Tags { "RenderType"="Opaque" } Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 #include "Lighting.cginc"
 sampler2D _ReflectionTex; float4x4 _ReflectionVP; float _RideTime;
 struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; UNITY_FOG_COORDS(1) };
 v2f vert(appdata_base v) { v2f o; o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.pos=UnityObjectToClipPos(v.vertex); UNITY_TRANSFER_FOG(o,o.pos); return o; }
 float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
 float noise(float2 p) { float2 a=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y); }
 float height(float2 p) { float2 flow=float2(_RideTime*.16,-_RideTime*.08); return noise(p*.32+flow)*.40+noise(p*.91-flow)*.10+noise(p*2.3+flow)*.022; }
 float4 frag(v2f i):SV_Target {
 float2 p=i.world.xz;
 float distance=length(_WorldSpaceCameraPos-i.world);
 float sampleSize=max(.12,length(fwidth(p))*.8);
 float2 gradient=float2(height(p+float2(sampleSize,0))-height(p-float2(sampleSize,0)),height(p+float2(0,sampleSize))-height(p-float2(0,sampleSize)))/(2*sampleSize);
 float3 normal=normalize(float3(-gradient.x,1,-gradient.y));
 float3 view=normalize(_WorldSpaceCameraPos-i.world);
 float fresnel=.025+.975*pow(1-saturate(dot(normal,view)),5);
 float4 projected=mul(_ReflectionVP,float4(i.world,1));
 float2 uv=projected.xy/projected.w*.5+.5;
 float3 reflected=tex2D(_ReflectionTex,clamp(uv+gradient*.014,.002,.998)).rgb;
 float3 halfVector=normalize(view+normalize(_WorldSpaceLightPos0.xyz));
 float glint=pow(saturate(dot(normal,halfVector)),180)*2.4;
 float3 water=lerp(float3(.006,.022,.15),float3(.015,.065,.32),saturate(normal.y*.6));
 float4 color=float4(lerp(water,reflected,lerp(.22,.78,fresnel))+_LightColor0.rgb*glint,1);
 float center=sin(p.y*.012)*19+sin(p.y*.028)*5;
 float shore=-26-sin(p.y*.021)*8-sin(p.y*.057)*4;
 float coastDistance=max(0,p.x-(-center-shore+1.55));
 float shallows=1-smoothstep(0,9,coastDistance);
 color.rgb=lerp(color.rgb,float3(.035,.19,.23),shallows*.32);
 float foam=(1-smoothstep(.15,1.5,coastDistance))*(.35+.65*noise(p*.9+_RideTime*.15));
 color.rgb=lerp(color.rgb,float3(.55,.68,.72),foam*.45);
 // Water spans a large primitive: interpolate world position, not corner fog values.
 UNITY_CALC_FOG_FACTOR_RAW(distance); color.rgb=lerp(unity_FogColor.rgb,color.rgb,saturate(unityFogFactor)); return color;
 }
 ENDCG
 } }
}
