// See "Unlit Transparent.shader" in Part 2 for detailed comments. Nothing has changed here.
Shader "ScratchPart5/Unlit Transparent"
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

struct VertexInput
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

VertexOutput Vertex(VertexInput v)
{
    VertexOutput o;
    float3 positionWS = TransformObjectToWorld(v.positionOS);
    o.positionCS = TransformWorldToHClip(positionWS);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

float4 Fragment(VertexOutput i) : SV_Target
{
    // Sample the main texture, and apply the tint (both are set in the material)
    return float4(tex2D(_MainTex, i.uv).rgb, 1.0) * _Tint;
}
ENDHLSL
        }
    }
}