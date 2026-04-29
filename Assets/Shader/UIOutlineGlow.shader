Shader "Custom/UIOutlineGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,1,1,1)
        _OutlineSize ("Outline Size (px)", Range(0,20)) = 3
        _GlowIntensity ("Glow Intensity", Range(0,3)) = 1.2
        _GlowFalloff ("Glow Falloff", Range(0.5,4)) = 1.5
        _Pulse ("Pulse Speed", Range(0,5)) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _OutlineColor;
            float _OutlineSize, _GlowIntensity, _GlowFalloff, _Pulse;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                float2 px = _MainTex_TexelSize.xy;
                float glow = 0;

                // 16 radial samples at 2 distances for smooth falloff
                [unroll]
                for (int k = 0; k < 16; k++) {
                    float ang = k * 0.3927; // 2*PI/16
                    float2 dir = float2(cos(ang), sin(ang));
                    float a1 = tex2D(_MainTex, i.uv + dir * px * _OutlineSize * 0.5).a;
                    float a2 = tex2D(_MainTex, i.uv + dir * px * _OutlineSize).a;
                    glow += a1 * 0.7 + a2 * 0.3;
                }
                glow = saturate(glow / 16.0);
                glow = pow(glow, _GlowFalloff);

                float pulse = sin(_Time.y * _Pulse) * 0.2 + 0.8;
                fixed4 outline = _OutlineColor * glow * _GlowIntensity * pulse;
                outline.a *= (1 - c.a); // only show outside sprite

                return c + outline;
            }
            ENDCG
        }
    }
}