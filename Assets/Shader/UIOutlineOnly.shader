Shader "Custom/UIOutlineOnly"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,0.9,0.3,1)
        _OutlineThickness ("Outline Thickness (px)", Range(1,10)) = 3
        _GlowSize ("Glow Spread (px)", Range(0,15)) = 5
        _GlowIntensity ("Glow Intensity", Range(0,3)) = 1.5
        _Pulse ("Pulse Speed", Range(0,15)) = 0
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
            float _OutlineThickness, _GlowSize, _GlowIntensity, _Pulse;

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

                // Find nearest opaque neighbor distance (outside sprite)
                float minDist = 999;
                [unroll]
                for (int k = 0; k < 16; k++) {
                    float ang = k * 0.3927;
                    float2 dir = float2(cos(ang), sin(ang));
                    [unroll]
                    for (int s = 1; s <= 4; s++) {
                        float d = s * (_GlowSize / 4.0);
                        float a = tex2D(_MainTex, i.uv + dir * px * d).a;
                        if (a > 0.5) { minDist = min(minDist, d); break; }
                    }
                }

                float pulse = _Pulse > 0 ? sin(_Time.y * _Pulse) * 0.2 + 0.8 : 1;

                // Inside sprite: render normally
                if (c.a > 0.5) return c;

                // Outside sprite: only glow within outline thickness + falloff
                if (minDist >= 999) return fixed4(0,0,0,0);

                float ring = 1.0 - saturate(minDist / _GlowSize);
                ring = pow(ring, 2.0); // soft falloff
                
                fixed4 glow = _OutlineColor;
                glow.a = ring * _GlowIntensity * pulse;
                return glow;
            }
            ENDCG
        }
    }
}