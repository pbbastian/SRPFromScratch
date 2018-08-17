// See "Unlit.shader" in Part 2 for detailed comments. Nothing has changed here.
Shader "ScratchPart4/Unlit"
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
            "RenderType"="Opaque"
            "LightMode" = "Forward"
        }

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
    // Object-space position
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct VertexOutput
{
    // Clip-space position
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

VertexOutput Vertex(VertexInput v)
{
    // Transform object-space position into clip-space, and process the UV coords as usual.
    VertexOutput o;
    float3 positionWS = TransformObjectToWorld(v.positionOS);
    o.positionCS = TransformWorldToHClip(positionWS);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

float4 Fragment(VertexOutput i) : SV_Target
{
    // Sample the main texture, and apply the tint (both are set in the material)
    return float4(tex2D(_MainTex, i.uv).rgb * _Tint.xyz, 1.0);
}
ENDHLSL
        }
    }
}