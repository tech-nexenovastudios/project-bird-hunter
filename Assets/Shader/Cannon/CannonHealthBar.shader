Shader "UI/HealthBarColor"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _Health ("Health (0-1)", Range(0,1)) = 1
        _ColorFull ("Full Color", Color) = (0,1,0,1)     // green
        _ColorEmpty ("Empty Color", Color) = (1,0,0,1)   // red
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Health;
            float4 _ColorFull;
            float4 _ColorEmpty;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

fixed4 frag (v2f i) : SV_Target
{
    fixed4 texCol = tex2D(_MainTex, i.uv);

    float h = saturate(_Health);

    fixed4 col;
    if (h > 0.5)
    {
        // 0.5–1.0 : yellow → green
        float t = (h - 0.5) / 0.5;
        fixed4 yellow = fixed4(1, 1, 0, 1);
        col = lerp(yellow, _ColorFull, t);   // yellow→green
    }
    else
    {
        // 0.0–0.5 : red → yellow
        float t = h / 0.5;
        fixed4 yellow = fixed4(1, 1, 0, 1);
        col = lerp(_ColorEmpty, yellow, t);  // red→yellow
    }

    return texCol * col;
}
            ENDCG
        }
    }
}