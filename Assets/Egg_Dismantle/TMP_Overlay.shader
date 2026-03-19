Shader "Custom/TMP_Overlay"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
        _Softness ("Softness", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "OverlayPass"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _FaceColor;
            float _FaceDilate;
            float _Softness;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float ComputeWidth(float dist)
            {
                float dx = abs(ddx(dist));
                float dy = abs(ddy(dist));
                return (dx + dy) * _Softness + 0.001;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = tex2D(_MainTex, i.uv).a;

                float width = ComputeWidth(dist);

                float d = dist + _FaceDilate;

                float alpha = smoothstep(0.5 - width, 0.5 + width, d);

                fixed4 col = _FaceColor * i.color;
                col.a *= alpha;

                return col;
            }

            ENDCG
        }
    }
}