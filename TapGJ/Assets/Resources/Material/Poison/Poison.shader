Shader "Custom/Bullet/Poison"
{
    Properties
    {
        [MainTexture] _MainTex("Main Tex", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)

        _MainPoisonColor("Main Poison Color", Color) = (0.35, 0.95, 0.2, 1.0)
        _DarkPoisonColor("Dark Poison Color", Color) = (0.1, 0.35, 0.08, 1.0)
        _BubbleColor("Bubble Color", Color) = (0.8, 1.0, 0.6, 1.0)

        _FlowSpeed("Flow Speed", Float) = 1.4
        _DistortStrength("Distort Strength", Float) = 0.025
        _BubbleDensity("Bubble Density", Float) = 18.0
        _BubbleStrength("Bubble Strength", Float) = 0.35
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
                float4 _MainPoisonColor;
                float4 _DarkPoisonColor;
                float4 _BubbleColor;
                float _FlowSpeed;
                float _DistortStrength;
                float _BubbleDensity;
                float _BubbleStrength;
                float _AlphaMul;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

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

                float2 centered = uv - 0.5;
                float dist = length(centered) * 2.0;

                // 黏液流动
                uv.x += sin((uv.y * 10.0) + t * 2.5) * _DistortStrength;
                uv.y += sin((uv.x * 7.0) - t * 1.8) * (_DistortStrength * 0.6);

                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // 深浅混合
                float slime = sin((uv.x + t * 0.4) * 8.0) * 0.5 + 0.5;
                slime += sin((uv.y - t * 0.7) * 11.0) * 0.5 + 0.5;
                slime *= 0.5;

                float3 col = lerp(_DarkPoisonColor.rgb, _MainPoisonColor.rgb, slime);

                // 气泡
                float2 bubbleUV = uv * _BubbleDensity;
                float2 cell = floor(bubbleUV);
                float2 local = frac(bubbleUV) - 0.5;

                float rnd = hash21(cell);
                float2 offset = float2(
                    sin(t + rnd * 6.28),
                    cos(t * 0.8 + rnd * 6.28)
                ) * 0.15;

                float bubble = 1.0 - smoothstep(0.08, 0.18, length(local + offset));
                bubble *= step(0.75, rnd);
                col = lerp(col, _BubbleColor.rgb, bubble * _BubbleStrength);

                // 边缘弱透明
                float alpha = tex.a * _BaseColor.a * _AlphaMul;
                alpha *= smoothstep(1.12, 0.6, dist);

                return half4(col * tex.rgb * i.color.rgb, alpha * i.color.a);
            }
            ENDHLSL
        }
    }
}