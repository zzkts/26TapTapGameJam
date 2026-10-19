Shader "TapGJ/Liquid Drop 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Kind ("Drop / Ribbon / Spray / Impact", Float) = 0
        _Phase ("Motion Phase", Float) = 0
        _Progress ("Impact Age", Range(0,1)) = 0
        _Electric ("Electricity", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Kind, _Phase, _Progress, _Electric;
        CBUFFER_END
        struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
        struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
        Varyings Vert(Attributes v)
        {
            Varyings o;
            o.positionCS=TransformObjectToHClip(v.positionOS);
            o.uv=v.uv;
            o.color=v.color*_Color;
            return o;
        }
        float SoftUnion(float a, float b, float k)
        {
            float h=saturate(.5+.5*(b-a)/k);
            return lerp(b,a,h)-k*h*(1-h);
        }
        half4 Frag(Varyings i):SV_Target
        {
            float2 p=i.uv-.5;
            float t=_Time.y*13+_Phase;
            if (_Kind>.5 && _Kind<1.5)
            {
                // A translucent, uneven water ligament; neither additive glow nor a solid laser.
                float wobble=sin(i.uv.x*22-t)*.055+sin(i.uv.x*39+t*.7)*.025;
                float edge=abs(p.y+wobble);
                float a=(1-smoothstep(.19,.49,edge))*(.46+.12*sin(i.uv.x*31-t));
                float glint=exp(-pow((p.y+wobble-.17)*24,2))*.42;
                return half4(lerp(float3(.13,.65,.86),float3(.86,1,1),glint)*i.color.rgb,a*i.color.a);
            }
            if (_Kind>3.5)
            {
                // Thin, rounded wet footprint with broken glints, soft edges and a dry-out fade.
                float edgeNoise=sin(p.x*28+sin(p.x*13)*2)*.025;
                float r=length(float2(p.x*2.14,p.y*2.7+edgeNoise));
                float mask=1-smoothstep(.72,1,r);
                float spread=lerp(.72,1,saturate(_Progress*12));
                mask*=1-smoothstep(.75*spread,1.02*spread,abs(p.x)*2);
                float glint=exp(-pow((p.y-.08-sin(p.x*16)*.035)*23,2));
                glint*=smoothstep(-.40,-.24,p.x)*(1-smoothstep(.17,.36,p.x));
                glint*=.4+.6*smoothstep(-.4,.5,sin(p.x*38));
                float dry=1-smoothstep(.35,1,_Progress);
                float3 water=lerp(float3(.10,.48,.62),float3(.65,.94,.98),glint*.6);
                float spark=pow(saturate(sin(p.x*55+_Time.y*18)),12)*_Electric;
                water=lerp(water,float3(.80,.92,1),spark);
                return half4(water*i.color.rgb,mask*(.23+glint*.26+spark*.25)*dry*i.color.a);
            }
            if (_Kind>2.5)
            {
                // Brief sheet of water leaving the actual contact surface, with an expanding rim.
                float2 q=float2(p.x,p.y+.35);
                float r=length(q/float2(1,.7));
                float radius=lerp(.08,.52,sqrt(_Progress));
                float rim=1-smoothstep(.012,.05,abs(r-radius));
                float sheet=(1-smoothstep(radius-.08,radius,r))*.16;
                float alpha=(rim*.58+sheet)*(1-_Progress)*step(0,q.y);
                return half4(lerp(float3(.15,.7,.9),float3(.8,.98,1),rim)*i.color.rgb,alpha*i.color.a);
            }
            float spray=step(1.5,_Kind);
            float2 warped=p;
            warped.y+=sin(p.x*12+t)*.012*(1-spray);
            float head=length((warped-float2(.075,0))/float2(.31,.33))-1;
            float tail=length((warped+float2(.14,0))/float2(.30,.18))-1;
            float d=lerp(SoftUnion(head,tail,.18),length(p)/.44-1,spray);
            float aa=max(fwidth(d)*1.15,.012);
            float mask=1-smoothstep(-aa,aa,d);
            float2 nxy=lerp((warped-float2(.025,0))/float2(.40,.35),p/.44,spray);
            float nz=sqrt(saturate(1-dot(nxy,nxy)));
            float3 n=normalize(float3(nxy,nz+.025));
            float fresnel=pow(1-nz,2);
            float diffuse=saturate(dot(n,normalize(float3(-.45,.65,1))));
            float3 water=lerp(float3(.025,.30,.57),float3(.18,.77,.92),diffuse);
            water=lerp(water,float3(.48,.9,.98),fresnel*.65);
            float spec=pow(saturate(dot(n,normalize(float3(-.36,.43,1)))),125);
            float crescent=exp(-pow((length((p-float2(-.03,.035))/float2(.31,.28))-.83)*25,2));
            crescent*=smoothstep(.02,.12,p.y)*(1-smoothstep(-.04,.10,p.x));
            float rim=exp(-pow((d+.055)*19,2));
            float upper=saturate(n.y*.8-n.x*.45+.2);
            water=lerp(water,float3(.91,.99,1),saturate(spec+crescent*.88+rim*upper*.8));
            float refraction=sin(p.x*18-p.y*10+t*.32)*.014;
            float innerArc=exp(-pow((length((p-float2(.02,-.02))/float2(.33,.26))-.80+refraction)*34,2));
            water+=innerArc*saturate(-p.y*6)*float3(.07,.18,.19);
            float lower=exp(-pow((p.y+.20)*28,2))*(1-smoothstep(.14,.35,abs(p.x)));
            water+=lower*float3(.11,.2,.17);
            float opacity=lerp(.34,.84,fresnel)+spec*.32+crescent*.25+rim*upper*.19;
            return half4(water*i.color.rgb,mask*saturate(opacity)*i.color.a);
        }
        ENDHLSL
        Pass
        {
            Name "Liquid2D"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Name "LiquidForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
