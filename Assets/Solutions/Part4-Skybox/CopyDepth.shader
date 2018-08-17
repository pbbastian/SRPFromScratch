Shader "ScratchPart4/CopyDepth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
#pragma vertex Vertex
#pragma fragment Fragment
#include "Scratch/Scratch.hlsl"

struct VertexInput
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct VertexOutput
{
    half4 positionCS : SV_POSITION;
    half2 uv : TEXCOORD0;
};

VertexOutput Vertex(VertexInput i)
{
    VertexOutput o;
    
    o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
    o.uv = i.uv;

    return o;
}

TEXTURE2D_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

float Fragment(VertexOutput i) : SV_Depth
{
     return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);
}
ENDHLSL
        }
    }
}