// See "Lit.shader" in Part 3 for detailed comments. No changes here.
Shader "ScratchPart5/Lit"
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
        }

        Pass
        {
            Tags
            {
                // Remember to change this so that it gets rendered during the GBuffer pass instead.
                "LightMode" = "GBuffer"
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

// Struct representing the render targets of our GBuffer.
struct GBufferOutput
{
    float4 rt0 : SV_TARGET0;
    float4 rt1 : SV_TARGET1;
};

GBufferOutput Fragment(VertexOutput IN)
{
    // Get the abledo like before.
    float3 albedo = tex2D(_MainTex, IN.uv).rgb * _Tint.xyz;
    // Build up our output to the render targets.
    GBufferOutput output;
    output.rt0 = float4(albedo, 1.0);
    // Simply normal encoding that just transforms from [-1,1] to [0,1].
    // Remember to do the reverse when reading the GBuffer later.
    output.rt1 = float4(IN.normalWS, 0.0) * 0.5 + 0.5;
    // All done!
    return output;
}
ENDHLSL
        }
    }
}