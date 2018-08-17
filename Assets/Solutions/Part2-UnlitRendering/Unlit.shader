Shader "ScratchPart2/Unlit"
{
    Properties
    {
        // We let our shader have two properties: A texture and a color to tint the texture with.
        // You're free to do whatever you want here though :) 
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            // Specify that this is a shader for opaque rendering
            "RenderType"="Opaque"
            "LightMode" = "Forward"
        }

        Pass
        {
            Tags
            {
                // Note how this matches the pass name we put into our draw settings previously.
                "LightMode" = "Forward"
            }
            // The difference between this and CGPROGRAM is that HLSLSupport and UnityShaderVariables won't be included.
            // We don't need those since we're using the new shader library. 
            HLSLPROGRAM
#pragma vertex Vertex
#pragma fragment Fragment
// Include shader library we're going to use in this workshop.
// This also pulls in parts of the new shader library.
#include "Scratch/Scratch.hlsl"

// Declaring our per-material properties in a constant buffer will allow the new SRP batcher to function.
// It's a good habit to get into early, as it's an easy performance win that doesn't obscure your code.
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
    // In our vertex shader we just need to transform position into clip-space, and process the UV coords as usual.
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