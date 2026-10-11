Shader "Custom/Bullet/Water"
{
    Properties
    {
        [MainTexture] _MainTex("Main Tex", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (0.2, 0.7, 1.0, 1.0)
        _HighlightColor("Highlight Color", Color) = (0.8, 0.95, 1.0, 1.0)

        _FlowSpeed("Flow Speed", Float) = 2.0
        _WaveStrength("Wave Strength", Float) = 0.03
        _WaveFrequency("Wave Frequency", Float) = 12.0
        _RimPower("Rim Power", Float) = 2.0
        _AlphaMul("Alpha Mul", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _HighlightColor;
                float _FlowSpeed;
                float _WaveStrength;
                float _WaveFrequency;
                float _RimPower;
                float _AlphaMul;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y * _FlowSpeed;

                // 中心坐标
                float2 centered = uv - 0.5;
                float dist = length(centered) * 2.0;

                // 流动波形
                float wave1 = sin((uv.y + t) * _WaveFrequency) * _WaveStrength;
                float wave2 = sin((uv.x * 1.7 - t * 1.3) * (_WaveFrequency * 0.8)) * (_WaveStrength * 0.7);

                uv.x += wave1;
                uv.y += wave2;

                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // 简单水纹高光
                float highlight = sin((uv.y - t * 1.5) * 20.0) * 0.5 + 0.5;
                highlight *= smoothstep(0.9, 0.2, dist);

                // 边缘透明 + 中心亮
                float rim = pow(saturate(1.0 - dist), _RimPower);

                float3 col = _BaseColor.rgb * tex.rgb;
                col = lerp(col, _HighlightColor.rgb, highlight * 0.35 + rim * 0.4);

                float alpha = tex.a * _BaseColor.a * _AlphaMul;
                alpha *= smoothstep(1.15, 0.65, dist);

                return half4(col * i.color.rgb, alpha * i.color.a);
            }
            ENDHLSL
        }
    }
}