Shader "Custom/Reaction/Explosion"
{
    Properties
    {
        [MainTexture] _MainTex("Main Tex", 2D) = "white" {}
        _InnerColor("Inner Color", Color) = (1, 1, 0.72, 1)
        _OuterColor("Outer Color", Color) = (1, 0.16, 0.015, 1)
        _Progress("Progress", Range(0, 1)) = 0
        _RingWidth("Ring Width", Range(0.01, 0.3)) = 0.12
        _Distort("Edge Distort", Range(0, 0.2)) = 0.06
        _Alpha("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
        struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _InnerColor;
            float4 _OuterColor;
            float _Progress;
            float _RingWidth;
            float _Distort;
            float _Alpha;
        CBUFFER_END

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionHCS = TransformObjectToHClip(input.positionOS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            output.color = input.color;
            return output;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            float2 centered = input.uv - 0.5;
            float angle = atan2(centered.y, centered.x);
            float radius = length(centered) * 2.0;
            float noise = sin(angle * 11.0 + _Progress * 18.0) * _Distort;
            float ringRadius = lerp(0.08, 0.92, _Progress);
            float ring = 1.0 - smoothstep(_RingWidth * 0.35, _RingWidth,
                abs(radius + noise - ringRadius));
            float core = (1.0 - smoothstep(0.0, ringRadius, radius))
                * (1.0 - _Progress) * 0.8;
            float mask = saturate(ring + core);
            float fade = 1.0 - smoothstep(0.72, 1.0, _Progress);
            float3 color = lerp(_InnerColor.rgb, _OuterColor.rgb,
                saturate(radius / max(ringRadius, 0.01)));
            float texAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
            return half4(color * input.color.rgb,
                mask * fade * _Alpha * texAlpha * input.color.a);
        }
        ENDHLSL

        Pass
        {
            Name "Explosion2D"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "ExplosionForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
