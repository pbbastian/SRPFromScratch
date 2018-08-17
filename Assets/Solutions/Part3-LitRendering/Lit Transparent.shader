// See "Lit.shader" for detailed comments, as this heavily based on that one.
Shader "ScratchPart3/Lit Transparent"
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
            // Change to Transparent RenderType and Queue as before
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        
        // Turn off z-writes and set alpha blending appropriately as before
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
    // Object-space position
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
    // Store all the 4 components as we need the alpha value later.
    float4 mainTex = tex2D(_MainTex, IN.uv);
    float3 albedo = mainTex.rgb * _Tint.xyz;
    float3 color;
    
    for (int i = 0; i < _LightCount; i++) {
        LightData light = _LightBuffer[i];
        if (light.positionRangeOrDirectionWS.w < 0) {
            color += ShadeDirectionalLight(IN.normalWS, albedo, light.positionRangeOrDirectionWS.xyz, light.color);
        } else {
            color += ShadePointLight(IN.normalWS, albedo, IN.positionWS, light.positionRangeOrDirectionWS.xyz, light.positionRangeOrDirectionWS.w, light.color);
        }
    }
    
    // Finally, output the color we just calculated. We also provide an alpha value.
    return float4(color, mainTex.a * _Tint.a);
}
ENDHLSL
        }
    }
}