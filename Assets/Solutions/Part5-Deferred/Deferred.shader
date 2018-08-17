Shader "ScratchPart5/Deferred"
{
    SubShader
    {
        Pass
        {
            // No need to write into the depth buffer from the deferred pass.
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
#pragma vertex Vertex
#pragma fragment Fragment
#include "Scratch/Scratch.hlsl"

// Declare textures and samples for the GBuffer and Depth textures.
// TexelSize variables are automatically set by Unity for each texture.
TEXTURE2D(_GBuffer0); SAMPLER(sampler_GBuffer0); float4 _GBuffer0_TexelSize;
TEXTURE2D(_GBuffer1); SAMPLER(sampler_GBuffer1);
TEXTURE2D_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

// The same light data and buffers from Part 4.
struct LightData
{
    float4 positionRangeOrDirectionWS;
    float4 color;
};

StructuredBuffer<LightData> _LightBuffer;
int _LightCount;

float4 Vertex(float3 positionOS : POSITION) : SV_POSITION
{
    return TransformObjectToHClip(positionOS.xyz);
}

float4 Fragment(float4 positionCS : SV_POSITION) : SV_Target
{
    // Compute UV coordinates based on clip space position.
    float2 uv = positionCS.xy * _GBuffer0_TexelSize.xy;
    
    // Sample GBuffers for albedo and normal.
    float3 albedo = SAMPLE_TEXTURE2D(_GBuffer0, sampler_GBuffer0, uv).xyz;
    float3 normalWS = SAMPLE_TEXTURE2D(_GBuffer1, sampler_GBuffer1, uv).xyz * 2 - 1;
    
    // Same lighting loop as before, but using the values from the GBuffer instead.
    float3 color = 0;
    for (int i = 0; i < _LightCount; i++) {
        LightData light = _LightBuffer[i];
        if (light.positionRangeOrDirectionWS.w < 0) {
            color += ShadeDirectionalLight(normalWS, albedo, light.positionRangeOrDirectionWS.xyz, light.color);
        } else {
            // We reconstruct the world space position from the depth buffer.
            float3 positionWS = SampleDepthAsWorldPosition(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
            color += ShadePointLight(normalWS, albedo, positionWS, light.positionRangeOrDirectionWS.xyz, light.positionRangeOrDirectionWS.w, light.color);
        }
    }
    
    return float4(color, 1.0);
}

            ENDHLSL
        }
    }
}