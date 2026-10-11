Shader "Custom/Reaction/PoisonCloud"
{
    Properties
    {
        [MainTexture] _MainTex("Main Tex", 2D) = "white" {}
        _BrightColor("Bright Color", Color) = (0.48, 1, 0.16, 1)
        _DarkColor("Dark Color", Color) = (0.08, 0.22, 0.03, 1)
        _BubbleColor("Bubble Color", Color) = (0.82, 1, 0.4, 1)
        _FlowSpeed("Flow Speed", Float) = 0.55
        _Density("Density", Float) = 5
        _BubbleDensity("Bubble Density", Float) = 9
        _Alpha("Alpha", Range(0, 1)) = 0.68
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
            float4 _BrightColor;
            float4 _DarkColor;
            float4 _BubbleColor;
            float _FlowSpeed;
            float _Density;
            float _BubbleDensity;
            float _Alpha;
        CBUFFER_END

        float Hash(float2 p)
        {
            p = frac(p * float2(123.34, 345.45));
            p += dot(p, p + 34.345);
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
            float cloud = Noise(uv * _Density + float2(time, time * 0.45));
            cloud = cloud * 0.65 + Noise(uv * _Density * 2.1 - time * 0.7) * 0.35;

            float2 centered = (uv - 0.5) * float2(1, 1.25);
            float shape = 1.0 - smoothstep(0.36, 0.54,
                length(centered) + (cloud - 0.5) * 0.18);

            float2 bubbleUv = uv * _BubbleDensity;
            float2 cell = floor(bubbleUv);
            float2 local = frac(bubbleUv) - 0.5;
            float random = Hash(cell);
            local.y += frac(time + random) - 0.5;
            float bubble = (1.0 - smoothstep(0.08, 0.16, abs(length(local) - 0.12)))
                * step(0.72, random);

            float3 color = lerp(_DarkColor.rgb, _BrightColor.rgb, cloud);
            color = lerp(color, _BubbleColor.rgb, bubble * 0.8);
            float texAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            float alpha = shape * lerp(0.45, 1.0, cloud) * _Alpha * texAlpha * input.color.a;
            return half4(color * input.color.rgb, alpha);
        }
        ENDHLSL

        Pass
        {
            Name "PoisonCloud2D"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "PoisonCloudForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
