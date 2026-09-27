Shader "MoonlitRide/Fabric" {
 Properties { _Color ("Tint", Color) = (1,1,1,1) _MainTex ("Woven botanical print",2D) = "white" {} _Glossiness ("Smoothness",Range(0,1))=.2 }
 SubShader { Tags { "RenderType"="Opaque" } Cull Off
 CGPROGRAM
 #pragma surface surf Painted fullforwardshadows addshadow vertex:vert
 #pragma target 3.0
 #include "PaintedLighting.cginc"
 sampler2D _MainTex; fixed4 _Color; half _Glossiness;
 struct Input { float3 skirtCoordinates; float facing : VFACE; };
 void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.skirtCoordinates=float3(v.texcoord.xy,v.texcoord1.x); }
 void surf(Input IN, inout SurfaceOutput o) {
 float2 garmentUV=float2(atan2(IN.skirtCoordinates.x,IN.skirtCoordinates.y)/6.2831853,IN.skirtCoordinates.z);
 fixed4 c=tex2D(_MainTex,garmentUV)*_Color;o.Albedo=c.rgb;o.Gloss=_Glossiness;
 float weave=sin(garmentUV.x*6400)*sin(garmentUV.y*6400);
 o.Normal=normalize(float3(weave*.035,0,IN.facing>0?1:-1));o.Alpha=1;
 }
 ENDCG
 } Fallback "Diffuse"
}
