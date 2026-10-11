Shader "Custom/Reaction/Steam"
{
    Properties
    {
        [MainTexture] _MainTex("Main Tex", 2D) = "white" {}
        _LightColor("Light Color", Color) = (0.92, 0.96, 1, 1)
        _ShadowColor("Shadow Color", Color) = (0.48, 0.62, 0.7, 1)
        _FlowSpeed("Flow Speed", Float) = 0.35
        _Density("Density", Float) = 4
        _Softness("Edge Softness", Range(0.01, 0.4)) = 0.18
        _Alpha("Alpha", Range(0, 1)) = 0.55
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
            float4 _LightColor;
            float4 _ShadowColor;
            float _FlowSpeed;
            float _Density;
            float _Softness;
            float _Alpha;
        CBUFFER_END

        float Hash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float Noise(float2 p)
        {
            float2 cell = floor(p);
            float2 local = frac(p);
            local = local * local * (3.0 - 2.0 * local);
            return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), local.x),
                lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), local.x), local.y);
        }

        float Fbm(float2 p)
        {
            float value = Noise(p) * 0.6;
            value += Noise(p * 2.03 + 7.1) * 0.28;
            value += Noise(p * 4.11 + 13.7) * 0.12;
            return value;
        }

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
            float2 uv = input.uv;
            float time = _Time.y * _FlowSpeed;
            float2 flowUv = uv * _Density + float2(time * 0.65, time);
            float cloud = Fbm(flowUv + float2(sin(uv.y * 8 + time), 0));
            float2 centered = (uv - 0.5) * float2(1, 1.35);
            float shape = 1.0 - smoothstep(0.5 - _Softness, 0.5,
                length(centered) + (cloud - 0.5) * 0.16);
            float texAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            float alpha = shape * lerp(0.35, 1.0, cloud) * _Alpha * texAlpha * input.color.a;
            float3 color = lerp(_ShadowColor.rgb, _LightColor.rgb, cloud) * input.color.rgb;
            return half4(color, alpha);
        }
        ENDHLSL

        Pass
        {
            Name "Steam2D"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "SteamForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
