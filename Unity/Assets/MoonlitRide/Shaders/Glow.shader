Shader "MoonlitRide/Glow" {
 Properties { _Color ("Color", Color) = (1,.8,.4,1) }
 SubShader { Tags { "RenderType"="Opaque" } Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 fixed4 _Color;
 struct v2f { float4 pos:SV_POSITION; UNITY_FOG_COORDS(0) };
 v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); UNITY_TRANSFER_FOG(o,o.pos); return o; }
 fixed4 frag(v2f i):SV_Target { fixed4 c=_Color; UNITY_APPLY_FOG(i.fogCoord,c); return c; }
 ENDCG
 } }
}
