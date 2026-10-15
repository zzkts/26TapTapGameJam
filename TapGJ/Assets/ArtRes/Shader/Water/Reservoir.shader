Shader "TapGJ/Reservoir 2D"
{
    Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Pollution("Pollution",Range(0,1))=0 _Charged("Charged",Float)=0 }
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        float _Pollution,_Charged;
        struct A{float3 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
        V vert(A i){V o;o.p=TransformObjectToHClip(i.p);o.uv=i.uv;return o;}
        half4 frag(V i):SV_Target
        {
            float wave=sin(i.uv.x*31+_Time.y*2.5)*.015+sin(i.uv.x*15-_Time.y*1.6)*.012;
            float surface=.965+wave;
            float a=(1-smoothstep(surface-.008,surface+.012,i.uv.y))*.48;
            float foam=1-smoothstep(.003,.022,abs(i.uv.y-surface));
            float caustic=pow(saturate(sin(i.uv.x*42+_Time.y*1.2)*sin(i.uv.y*19-_Time.y)),9);
            float3 clean=lerp(float3(.04,.34,.60),float3(.24,.92,.95),i.uv.y);
            float3 dirty=lerp(float3(.18,.32,.12),float3(.64,.87,.32),i.uv.y);
            float3 col=lerp(clean,dirty,_Pollution*.85)+caustic*.1;
            col=lerp(col,float3(.68,1,.95),foam*.8);
            float spark=pow(saturate(sin(i.uv.x*65+sin(i.uv.y*31+_Time.y*17))),22)*_Charged;
            col=lerp(col,float3(1,.96,.54),spark);
            return half4(col,saturate(a+foam*.22+spark*.12));
        }
        ENDHLSL
        Pass{Tags{"LightMode"="Universal2D"}HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        ENDHLSL}
        Pass{Tags{"LightMode"="UniversalForward"}HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        ENDHLSL}
    }
}
