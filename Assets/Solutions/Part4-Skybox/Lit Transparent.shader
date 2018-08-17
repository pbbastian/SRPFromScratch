// See "Lit Transparent.shader" in Part 3 for detailed comments. No changs here.
Shader "ScratchPart4/Lit Transparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags
            {
                "LightMode" = "Forward"
            }
            HLSLPROGRAM
#pragma vertex Vertex
#pragma fragment Fragment
#include "Scratch/Scratch.hlsl"

CBUFFER_START(UnityPerMaterial)
sampler2D _MainTex;
float4 _MainTex_ST;
float4 _Tint;
CBUFFER_END

struct LightData
{
    float4 positionRangeOrDirectionWS;
    float4 color;
};

StructuredBuffer<LightData> _LightBuffer;
int _LightCount;

struct VertexInput
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float2 uv : TEXCOORD0;
};

VertexOutput Vertex(VertexInput v)
{
    VertexOutput o;
    o.positionWS = TransformObjectToWorld(v.positionOS);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

float4 Fragment(VertexOutput IN) : SV_Target
{
    float3 albedo = tex2D(_MainTex, IN.uv).rgb * _Tint.xyz;
    float3 color;
    
    for (int i = 0; i < _LightCount; i++) {
        LightData light = _LightBuffer[i];
        if (light.positionRangeOrDirectionWS.w < 0) {
            color += ShadeDirectionalLight(IN.normalWS, albedo, light.positionRangeOrDirectionWS.xyz, light.color);
        } else {
            color += ShadePointLight(IN.normalWS, albedo, IN.positionWS, light.positionRangeOrDirectionWS.xyz, light.positionRangeOrDirectionWS.w, light.color);
        }
    }
    
    return float4(color, _Tint.a);
}
ENDHLSL
        }
    }
}