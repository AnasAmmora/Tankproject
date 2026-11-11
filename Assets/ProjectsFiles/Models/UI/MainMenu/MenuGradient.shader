Shader "TanksAway/MenuGradient" {
Properties {
    [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
    _Opacity ("Left opacity", Range(0,1)) = 0.76
}
SubShader {
Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
Cull Off ZWrite Off ZTest Always
Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
float _Opacity;
v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
fixed4 frag(v2f i):SV_Target { float a=(1-smoothstep(.25,1,i.uv.x))*_Opacity;return fixed4(.015,.02,.015,a*i.color.a); }
ENDCG
}
}
}

