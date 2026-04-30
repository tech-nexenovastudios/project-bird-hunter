Shader "Custom/UIBlur"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,0.5)
        _Size ("Blur Size", Range(0, 30)) = 5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha

        GrabPass { "_BackgroundTex" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 grabUV:TEXCOORD0; };

            sampler2D _BackgroundTex;
            float4 _BackgroundTex_TexelSize;
            fixed4 _Color;
            float _Size;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabUV = ComputeGrabScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.grabUV.xy / i.grabUV.w;
                float2 px = _BackgroundTex_TexelSize.xy * _Size;
                fixed4 col = 0;

                // 9-tap box blur
                col += tex2D(_BackgroundTex, uv + float2(-px.x, -px.y));
                col += tex2D(_BackgroundTex, uv + float2(    0, -px.y));
                col += tex2D(_BackgroundTex, uv + float2( px.x, -px.y));
                col += tex2D(_BackgroundTex, uv + float2(-px.x,    0));
                col += tex2D(_BackgroundTex, uv);
                col += tex2D(_BackgroundTex, uv + float2( px.x,    0));
                col += tex2D(_BackgroundTex, uv + float2(-px.x,  px.y));
                col += tex2D(_BackgroundTex, uv + float2(    0,  px.y));
                col += tex2D(_BackgroundTex, uv + float2( px.x,  px.y));

                col /= 9;
                return col * _Color;
            }
            ENDCG
        }
    }
}