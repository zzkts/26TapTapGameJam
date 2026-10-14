Shader "TapGJ/ElementSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Element ("Element", Float) = 0
        _Mode ("Projectile", Float) = 0
        _Flash ("Hit Flash", Range(0,1)) = 0
        _Electric ("Electricity", Range(0,1)) = 0
        _Field ("Area", Float) = 0
        _Progress ("Lifetime", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            sampler2D _MainTex;
            float4 _Color;
            float _Element, _Mode, _Flash, _Electric, _Field, _Progress;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; o.color = v.color * _Color;
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float time = _Time.y;
                float ripple = sin(i.uv.y * 28 + time * 4) * sin(i.uv.x * 17 - time * 3);
                float2 uv = i.uv + float2(ripple, sin(i.uv.x * 22 + time * 2)) * 0.009 * saturate(_Mode);
                float4 tex = tex2D(_MainTex, uv);
                float4 result = tex * i.color;
                float water = step(0.5, _Element) * (1-step(1.5, _Element));
                float fire = step(1.5, _Element) * (1-step(2.5, _Element));
                float poison = step(3.5, _Element);
                float flow = saturate(0.5 + ripple * 0.24 + (1-i.uv.y) * 0.22);
                float3 palette = lerp(float3(0.05,0.4,0.85), float3(0.6,0.97,1), flow);
                palette = lerp(palette, lerp(float3(0.95,0.15,0.04),float3(1,0.94,0.27),flow),fire);
                palette = lerp(palette, lerp(float3(0.12,0.34,0.08),float3(0.65,1,0.18),flow),poison);
                float active = saturate(water + fire + poison);
                result.rgb = lerp(result.rgb, palette, active * lerp(0.28,0.8,saturate(_Mode)));
                float bubble = smoothstep(0.85,0.98,sin(i.uv.x*32+time*2)*sin(i.uv.y*26-time*3));
                result.rgb += bubble * poison * saturate(_Mode) * 0.22;
                float flame = pow(saturate(sin(i.uv.x*25 + time*8) * 0.5 + 0.5), 5) * i.uv.y;
                result.rgb += fire * flame * saturate(_Mode) * float3(0.3,0.2,0);
                if (_Field > 0.5)
                {
                    float edge = 1-smoothstep(0.28,0.52,length(i.uv-0.5) + ripple * 0.025);
                    result.a *= edge * lerp(0.45,1,saturate(_Progress));
                }
                float bolt = 1-smoothstep(0.006,0.025,abs(frac(i.uv.x*5 + sin(i.uv.y*23+time*16)*0.15)-0.5));
                result.rgb = lerp(result.rgb,float3(0.8,0.91,1),bolt * _Electric);
                result.rgb += float3(0.12,0.14,0.4) * bolt * _Electric;
                result.rgb = lerp(result.rgb,1,_Flash);
                return result;
            }
            ENDHLSL
        }
    }
}
