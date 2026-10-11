Shader "Custom/Bullet/Fire"
{
    Properties
    {
        [MainTexture] _MainTex("Main Tex", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)

        _InnerColor("Inner Color", Color) = (1.0, 0.9, 0.2, 1.0)
        _MidColor("Mid Color", Color) = (1.0, 0.45, 0.1, 1.0)
        _OuterColor("Outer Color", Color) = (0.8, 0.05, 0.02, 1.0)

        _FlameSpeed("Flame Speed", Float) = 2.5
        _DistortStrength("Distort Strength", Float) = 0.035
        _FlickerStrength("Flicker Strength", Float) = 0.15
        _AlphaMul("Alpha Mul", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
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
                float4 _InnerColor;
                float4 _MidColor;
                float4 _OuterColor;
                float _FlameSpeed;
                float _DistortStrength;
                float _FlickerStrength;
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
                float t = _Time.y * _FlameSpeed;

                float2 centered = uv - 0.5;
                float dist = length(centered) * 2.0;

                // 火焰扰动：越靠上抖动越明显
                float distort = sin((uv.y * 12.0) - t * 5.0 + uv.x * 8.0) * _DistortStrength;
                distort += sin((uv.y * 20.0) + t * 3.5) * (_DistortStrength * 0.5);
                uv.x += distort * (uv.y + 0.2);

                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // 闪烁
                float flicker = 1.0 + sin(t * 8.0 + uv.y * 14.0) * _FlickerStrength;

                // 从内到外渐变
                float heat = saturate(1.0 - dist);
                float3 fireCol = lerp(_OuterColor.rgb, _MidColor.rgb, heat);
                fireCol = lerp(fireCol, _InnerColor.rgb, heat * heat);

                // 火舌感：顶部更活跃
                float flameMask = saturate(1.0 - dist + uv.y * 0.2);
                fireCol *= flicker * (0.8 + flameMask * 0.4);

                float alpha = tex.a * _BaseColor.a * _AlphaMul;
                alpha *= smoothstep(1.15, 0.55, dist);
                alpha *= (0.8 + flameMask * 0.2);

                return half4(fireCol * tex.rgb * i.color.rgb, alpha * i.color.a);
            }
            ENDHLSL
        }
    }
}