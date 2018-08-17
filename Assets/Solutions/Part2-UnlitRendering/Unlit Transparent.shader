// This is heavily based on the Unlit shader for opaque objects. For the sake of simplicity we just create a copy of it and modify it,
// so when you do this you can just make a copy of your own Unlit shader, and work off of that. 
// Therefor the comments here are only for the changed things. Please see Unlit.shader for more comments :)
Shader "ScratchPart2/Unlit Transparent"
{
    Properties
    {
        // Same properties as before.
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            // Notice that the RenderType has changed, and that the Queue is now explicitly set to "Transparent".
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        
        // We need to turn off z-writes for transparent rendering to work properly.
        ZWrite Off
        // We set-up the blending appropriately for transparent rendering.
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags
            {
                "LightMode" = "Forward"
            }
            HLSLPROGRAM
// Nothing has changed until we get to the Fragment function, so just scroll down, or go to Unlit.shader for comments.
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