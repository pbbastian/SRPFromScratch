Shader "ScratchPart3/Lit"
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

// This struct contains the data needed for each light.
struct LightData
{
    // Depending on whether it's a point light or a directional light, we store world-space position or direction in here, respectively.
    // If it's a directional light we store direction in the xyz components, and a negative value in the w component.
    // This allows us to identify whether it is a directional light.
    // If it's a point light we store position in the xyz components, and range in the w component.
    float4 positionRangeOrDirectionWS;
    // This is just the color of the light.
    float4 color;
};

// This allows us to access the ComputeBuffer we create and fill with data from C#.
StructuredBuffer<LightData> _LightBuffer;
// We need to know how much of the light buffer has usable data.
int _LightCount;

struct VertexInput
{
    // Object-space position
    float3 positionOS : POSITION;
    // We also require the normal for performing lighting.
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
};

struct VertexOutput
{
    // Clip-space position
    float4 positionCS : SV_POSITION;
    // We would like to perform lighting in world space, so we interpolate this value.
    float3 positionWS : TEXCOORD1;
    // We need the normal in world space to perform lighting, so let's also interpolate that value.
    float3 normalWS : TEXCOORD2;
    float2 uv : TEXCOORD0;
};

VertexOutput Vertex(VertexInput v)
{
    // Transform object-space position into clip-space, and process the UV coords as usual.
    VertexOutput o;
    // We were already calculating it either way, so lets just write directly into the output instead.
    o.positionWS = TransformObjectToWorld(v.positionOS);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    // Transform the object-space normal to world-space.
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

float4 Fragment(VertexOutput IN) : SV_Target
{
    // We'll use the value we were outputting in the unlit shader as our albedo.
    float3 albedo = tex2D(_MainTex, IN.uv).rgb * _Tint.xyz;
    // Here we accumulate the calculated light values.
    float3 color;
    
    for (int i = 0; i < _LightCount; i++) {
        // Fetch the light from the light buffer.
        LightData light = _LightBuffer[i];
        // Check whether the light is directional or point and shade it based on that.
        if (light.positionRangeOrDirectionWS.w < 0) {
            // We use this function in the accompanying shader library to perform shading,
            // so that we don't have to worry about that now.
            color += ShadeDirectionalLight(IN.normalWS, albedo, light.positionRangeOrDirectionWS.xyz, light.color);
        } else {
            color += ShadePointLight(IN.normalWS, albedo, IN.positionWS, light.positionRangeOrDirectionWS.xyz, light.positionRangeOrDirectionWS.w, light.color);
        }
    }
    
    // Finally, output the color we just calculated.
    return float4(color, 1.0);
}
ENDHLSL
        }
    }
}