Shader "Tank Project/Curved World/Water"
{
Properties
{
_WaterDarkColor("WaterDarkColor", Color) = (0, 0, 0, 1)
_WaterBrightColor("WaterBrightColor", Color) = (0, 0, 0, 1)
_ShoreFoamColor("ShoreFoamColor", Color) = (0, 0, 0, 1)
_ShoreFoamSpeed("ShoreFoamSpeed", Range(0, 0.1)) = 0
_ShoreFoamAmount("ShoreFoamAmount", Range(0, 1)) = 0
_WaterFoamColor("WaterFoamColor", Color) = (0, 0, 0, 1)
_WaterFoamSpeed("WaterFoamSpeed", Range(0, 0.4)) = 0
_WaterFoamAmount("WaterFoamAmount", Range(0, 1)) = 0
_WaveIntensity("WaveIntensity", Range(0, 1)) = 0
_Emission("Emission", Range(0, 1)) = 0
_Scale("Scale", Float) = 0
[ToggleUI]_BasedOnWorldScale("BasedOnWorldScale", Float) = 0
[HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
[HideInInspector]_QueueControl("_QueueControl", Float) = -1
[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
}
SubShader
{
Tags
{
"RenderPipeline"="UniversalPipeline"
"RenderType"="Transparent"
"UniversalMaterialType" = "Lit"
"Queue"="Transparent"
"DisableBatching"="False"
"ShaderGraphShader"="true"
"ShaderGraphTargetId"="UniversalLitSubTarget"
}
Pass
{
    Name "Universal Forward"
    Tags
    {
        "LightMode" = "UniversalForward"
    }

// Render State
Cull Back
Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
ZTest LEqual
ZWrite Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
#pragma multi_compile _ REFLECTION_PROBE_ROTATION
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _LIGHT_LAYERS
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma multi_compile_fragment _ _LIGHT_COOKIES
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_FORWARD
#define _SURFACE_TYPE_TRANSPARENT 1
#define _ALPHAPREMULTIPLY_ON 1
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion;
#endif
 float4 fogFactorAndVertexLight;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV : INTERP0;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV : INTERP1;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP2;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion : INTERP3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord : INTERP4;
#endif
 float4 tangentWS : INTERP5;
 float4 texCoord0 : INTERP6;
 float4 fogFactorAndVertexLight : INTERP7;
 float3 positionWS : INTERP8;
 float3 normalWS : INTERP9;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float _ShoreFoamAmount;
float4 _WaterDarkColor;
float4 _WaterBrightColor;
float4 _ShoreFoamColor;
float4 _WaterFoamColor;
float _WaterFoamSpeed;
float _WaterFoamAmount;
float _WaveIntensity;
float _ShoreFoamSpeed;
float _Emission;
float _Scale;
float _BasedOnWorldScale;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
Hash_Tchou_2_2_float(UV, UV);
return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}

void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
float2 g = floor(UV * CellDensity);
float2 f = frac(UV * CellDensity);
float t = 8.0;
float3 res = float3(8.0, 0.0, 0.0);
for (int y = -1; y <= 1; y++)
{
for (int x = -1; x <= 1; x++)
{
float2 lattice = float2(x, y);
float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
float d = distance(lattice + offset, f);
if (d < res.x)
{
res = float3(d, offset.x, offset.y);
Out = res.x;
Cells = res.y;
}
}
}
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
{
float2 i = floor(uv);
float2 f = frac(uv);
f = f * f * (3.0 - 2.0 * f);
uv = abs(frac(uv) - 0.5);
float2 c0 = i + float2(0.0, 0.0);
float2 c1 = i + float2(1.0, 0.0);
float2 c2 = i + float2(0.0, 1.0);
float2 c3 = i + float2(1.0, 1.0);
float r0; Hash_Tchou_2_1_float(c0, r0);
float r1; Hash_Tchou_2_1_float(c1, r1);
float r2; Hash_Tchou_2_1_float(c2, r2);
float r3; Hash_Tchou_2_1_float(c3, r3);
float bottomOfGrid = lerp(r0, r1, f.x);
float topOfGrid = lerp(r2, r3, f.x);
float t = lerp(bottomOfGrid, topOfGrid, f.y);
return t;
}

void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
{
float freq, amp;
Out = 0.0f;
freq = pow(2.0, float(0));
amp = pow(0.5, float(3-0));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(1));
amp = pow(0.5, float(3-1));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(2));
amp = pow(0.5, float(3-2));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_A_4_Float = 0;
float _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float);
float _Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean = _BasedOnWorldScale;
float _Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float = _Scale;
float3 _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3;
Unity_Branch_float3(_Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float.xxx), _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3);
float3 _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3, float3(6, 6, 6), _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3);
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float;
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float, (_Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3).x, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float);
float _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float;
Unity_OneMinus_float(_Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float);
float _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float;
Unity_Clamp_float(_OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float, float(0), float(1), _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float);
float _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float;
Unity_Subtract_float(_Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float, float(0.5), _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float);
float _Property_af5407b060274092886eaf4639817c3c_Out_0_Float = _WaveIntensity;
float _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float;
Unity_Multiply_float_float(_Property_af5407b060274092886eaf4639817c3c_Out_0_Float, IN.TimeParameters.y, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float);
float _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float;
Unity_Multiply_float_float(_Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float);
float _Add_8149863b52284f3fbe288315639f5849_Out_2_Float;
Unity_Add_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float);
float4 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4;
float3 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
float2 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2;
Unity_Combine_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float, _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float, float(0), _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2);
description.Position = _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
float Alpha;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4 = _WaterFoamColor;
float4 _Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4 = _WaterDarkColor;
float4 _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4 = _WaterBrightColor;
float _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float);
float _Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean = _BasedOnWorldScale;
float _Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float = _Scale;
float3 _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3;
Unity_Branch_float3(_Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float.xxx), _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3);
float3 _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3, float3(6, 6, 6), _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3);
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float;
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float, (_Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3).x, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float);
float _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float;
Unity_OneMinus_float(_Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float);
float _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float;
Unity_Add_float(_OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float, float(0), _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float);
float4 _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4;
Unity_Lerp_float4(_Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4, _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4, (_Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float.xxxx), _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4);
float _Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float = _WaterFoamAmount;
float _Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean = _BasedOnWorldScale;
float _Property_55986d71a90a4e388241f53331a78d58_Out_0_Float = _Scale;
float3 _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3;
Unity_Branch_float3(_Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_55986d71a90a4e388241f53331a78d58_Out_0_Float.xxx), _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3);
float3 _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3, float3(1, 1, 1), _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3);
float _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float = _WaterFoamSpeed;
float _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float, _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float);
float2 _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3.xy), (_Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float.xx), _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2);
float _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2, float(200), _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float);
float _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float;
Unity_Step_float(_Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float, _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float, _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float);
float4 _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4;
Unity_Lerp_float4(_Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4, _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4, (_Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float.xxxx), _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4);
float4 _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4 = _ShoreFoamColor;
float _Float_5aaea56370c94e908d531e7748575723_Out_0_Float = float(100);
float _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float;
Unity_Divide_float(_Float_5aaea56370c94e908d531e7748575723_Out_0_Float, float(20), _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float);
float _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float);
float4 _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4 = IN.ScreenPosition;
float _Split_195f72821d3a408c9a6d7c0280c12eb8_R_1_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[0];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_G_2_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[1];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_B_3_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[2];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[3];
float _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float;
Unity_Subtract_float(_SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float, _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float, _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float);
float _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float;
Unity_Saturate_float(_Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float, _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float);
float _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float;
Unity_OneMinus_float(_Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float, _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float);
float _Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float = _ShoreFoamAmount;
float _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
Unity_OneMinus_float(_Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float, _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float);
float _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float = _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
float _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float;
Unity_Subtract_float(_OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float, _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float, _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float);
float _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float;
Unity_Clamp_float(_Subtract_e65156af410049729091e8151a177cb1_Out_2_Float, float(0), float(1), _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float);
float _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float;
Unity_Multiply_float_float(_Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float, _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float, _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float);
float _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float = _ShoreFoamSpeed;
float _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float, _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float);
float2 _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float.xx), _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2);
float _Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean = _BasedOnWorldScale;
float _Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float = _Scale;
float3 _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3;
Unity_Branch_float3(_Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float.xxx), _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3);
float3 _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3, float3(300, 2, 2), _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3);
float _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2, (_Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3).x, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float);
float _Add_4e789725a9d5427e86993028f873799e_Out_2_Float;
Unity_Add_float(_Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float, _Add_4e789725a9d5427e86993028f873799e_Out_2_Float);
float _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float;
Unity_Step_float(float(0.81), _Add_4e789725a9d5427e86993028f873799e_Out_2_Float, _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float);
float4 _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4, _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4, (_Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float.xxxx), _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4);
float _Property_a41f89ba0a404d258a7307cb564b29f2_Out_0_Float = _Emission;
float4 _Multiply_a0863f588ffb43aab35fdd05439043c6_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4, (_Property_a41f89ba0a404d258a7307cb564b29f2_Out_0_Float.xxxx), _Multiply_a0863f588ffb43aab35fdd05439043c6_Out_2_Vector4);
float _Split_3508d8cecb3a4376858da493969c0a7e_R_1_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[0];
float _Split_3508d8cecb3a4376858da493969c0a7e_G_2_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[1];
float _Split_3508d8cecb3a4376858da493969c0a7e_B_3_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[2];
float _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[3];
surface.BaseColor = (_Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = (_Multiply_a0863f588ffb43aab35fdd05439043c6_Out_2_Vector4.xyz);
surface.Metallic = float(0);
surface.Smoothness = float(0.3);
surface.Occlusion = float(1);
surface.Alpha = _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.uv0 =                                        input.uv0;
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    



    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/Upstream/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "GBuffer"
    Tags
    {
        "LightMode" = "UniversalGBuffer"
    }

// Render State
Cull Back
Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
ZTest LEqual
ZWrite Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 4.5
#pragma exclude_renderers gles3 glcore
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
#pragma multi_compile _ REFLECTION_PROBE_ROTATION
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_GBUFFER
#define _FOG_FRAGMENT 1
#define _SURFACE_TYPE_TRANSPARENT 1
#define _ALPHAPREMULTIPLY_ON 1
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion;
#endif
 float4 fogFactorAndVertexLight;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV : INTERP0;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV : INTERP1;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP2;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion : INTERP3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord : INTERP4;
#endif
 float4 tangentWS : INTERP5;
 float4 texCoord0 : INTERP6;
 float4 fogFactorAndVertexLight : INTERP7;
 float3 positionWS : INTERP8;
 float3 normalWS : INTERP9;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float _ShoreFoamAmount;
float4 _WaterDarkColor;
float4 _WaterBrightColor;
float4 _ShoreFoamColor;
float4 _WaterFoamColor;
float _WaterFoamSpeed;
float _WaterFoamAmount;
float _WaveIntensity;
float _ShoreFoamSpeed;
float _Emission;
float _Scale;
float _BasedOnWorldScale;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
Hash_Tchou_2_2_float(UV, UV);
return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}

void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
float2 g = floor(UV * CellDensity);
float2 f = frac(UV * CellDensity);
float t = 8.0;
float3 res = float3(8.0, 0.0, 0.0);
for (int y = -1; y <= 1; y++)
{
for (int x = -1; x <= 1; x++)
{
float2 lattice = float2(x, y);
float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
float d = distance(lattice + offset, f);
if (d < res.x)
{
res = float3(d, offset.x, offset.y);
Out = res.x;
Cells = res.y;
}
}
}
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
{
float2 i = floor(uv);
float2 f = frac(uv);
f = f * f * (3.0 - 2.0 * f);
uv = abs(frac(uv) - 0.5);
float2 c0 = i + float2(0.0, 0.0);
float2 c1 = i + float2(1.0, 0.0);
float2 c2 = i + float2(0.0, 1.0);
float2 c3 = i + float2(1.0, 1.0);
float r0; Hash_Tchou_2_1_float(c0, r0);
float r1; Hash_Tchou_2_1_float(c1, r1);
float r2; Hash_Tchou_2_1_float(c2, r2);
float r3; Hash_Tchou_2_1_float(c3, r3);
float bottomOfGrid = lerp(r0, r1, f.x);
float topOfGrid = lerp(r2, r3, f.x);
float t = lerp(bottomOfGrid, topOfGrid, f.y);
return t;
}

void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
{
float freq, amp;
Out = 0.0f;
freq = pow(2.0, float(0));
amp = pow(0.5, float(3-0));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(1));
amp = pow(0.5, float(3-1));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(2));
amp = pow(0.5, float(3-2));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_A_4_Float = 0;
float _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float);
float _Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean = _BasedOnWorldScale;
float _Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float = _Scale;
float3 _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3;
Unity_Branch_float3(_Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float.xxx), _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3);
float3 _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3, float3(6, 6, 6), _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3);
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float;
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float, (_Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3).x, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float);
float _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float;
Unity_OneMinus_float(_Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float);
float _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float;
Unity_Clamp_float(_OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float, float(0), float(1), _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float);
float _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float;
Unity_Subtract_float(_Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float, float(0.5), _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float);
float _Property_af5407b060274092886eaf4639817c3c_Out_0_Float = _WaveIntensity;
float _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float;
Unity_Multiply_float_float(_Property_af5407b060274092886eaf4639817c3c_Out_0_Float, IN.TimeParameters.y, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float);
float _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float;
Unity_Multiply_float_float(_Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float);
float _Add_8149863b52284f3fbe288315639f5849_Out_2_Float;
Unity_Add_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float);
float4 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4;
float3 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
float2 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2;
Unity_Combine_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float, _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float, float(0), _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2);
description.Position = _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
float Alpha;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4 = _WaterFoamColor;
float4 _Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4 = _WaterDarkColor;
float4 _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4 = _WaterBrightColor;
float _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float);
float _Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean = _BasedOnWorldScale;
float _Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float = _Scale;
float3 _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3;
Unity_Branch_float3(_Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float.xxx), _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3);
float3 _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3, float3(6, 6, 6), _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3);
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float;
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float, (_Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3).x, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float);
float _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float;
Unity_OneMinus_float(_Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float);
float _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float;
Unity_Add_float(_OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float, float(0), _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float);
float4 _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4;
Unity_Lerp_float4(_Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4, _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4, (_Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float.xxxx), _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4);
float _Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float = _WaterFoamAmount;
float _Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean = _BasedOnWorldScale;
float _Property_55986d71a90a4e388241f53331a78d58_Out_0_Float = _Scale;
float3 _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3;
Unity_Branch_float3(_Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_55986d71a90a4e388241f53331a78d58_Out_0_Float.xxx), _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3);
float3 _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3, float3(1, 1, 1), _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3);
float _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float = _WaterFoamSpeed;
float _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float, _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float);
float2 _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3.xy), (_Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float.xx), _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2);
float _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2, float(200), _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float);
float _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float;
Unity_Step_float(_Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float, _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float, _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float);
float4 _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4;
Unity_Lerp_float4(_Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4, _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4, (_Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float.xxxx), _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4);
float4 _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4 = _ShoreFoamColor;
float _Float_5aaea56370c94e908d531e7748575723_Out_0_Float = float(100);
float _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float;
Unity_Divide_float(_Float_5aaea56370c94e908d531e7748575723_Out_0_Float, float(20), _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float);
float _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float);
float4 _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4 = IN.ScreenPosition;
float _Split_195f72821d3a408c9a6d7c0280c12eb8_R_1_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[0];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_G_2_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[1];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_B_3_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[2];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[3];
float _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float;
Unity_Subtract_float(_SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float, _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float, _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float);
float _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float;
Unity_Saturate_float(_Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float, _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float);
float _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float;
Unity_OneMinus_float(_Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float, _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float);
float _Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float = _ShoreFoamAmount;
float _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
Unity_OneMinus_float(_Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float, _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float);
float _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float = _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
float _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float;
Unity_Subtract_float(_OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float, _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float, _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float);
float _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float;
Unity_Clamp_float(_Subtract_e65156af410049729091e8151a177cb1_Out_2_Float, float(0), float(1), _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float);
float _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float;
Unity_Multiply_float_float(_Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float, _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float, _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float);
float _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float = _ShoreFoamSpeed;
float _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float, _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float);
float2 _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float.xx), _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2);
float _Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean = _BasedOnWorldScale;
float _Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float = _Scale;
float3 _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3;
Unity_Branch_float3(_Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float.xxx), _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3);
float3 _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3, float3(300, 2, 2), _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3);
float _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2, (_Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3).x, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float);
float _Add_4e789725a9d5427e86993028f873799e_Out_2_Float;
Unity_Add_float(_Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float, _Add_4e789725a9d5427e86993028f873799e_Out_2_Float);
float _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float;
Unity_Step_float(float(0.81), _Add_4e789725a9d5427e86993028f873799e_Out_2_Float, _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float);
float4 _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4, _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4, (_Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float.xxxx), _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4);
float _Property_a41f89ba0a404d258a7307cb564b29f2_Out_0_Float = _Emission;
float4 _Multiply_a0863f588ffb43aab35fdd05439043c6_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4, (_Property_a41f89ba0a404d258a7307cb564b29f2_Out_0_Float.xxxx), _Multiply_a0863f588ffb43aab35fdd05439043c6_Out_2_Vector4);
float _Split_3508d8cecb3a4376858da493969c0a7e_R_1_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[0];
float _Split_3508d8cecb3a4376858da493969c0a7e_G_2_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[1];
float _Split_3508d8cecb3a4376858da493969c0a7e_B_3_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[2];
float _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[3];
surface.BaseColor = (_Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = (_Multiply_a0863f588ffb43aab35fdd05439043c6_Out_2_Vector4.xyz);
surface.Metallic = float(0);
surface.Smoothness = float(0.3);
surface.Occlusion = float(1);
surface.Alpha = _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.uv0 =                                        input.uv0;
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    



    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/Upstream/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "MotionVectors"
    Tags
    {
        "LightMode" = "MotionVectors"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask RG

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.5
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_TEXCOORD0
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_MOTION_VECTORS
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.positionWS.xyz = input.positionWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.positionWS = input.positionWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float _ShoreFoamAmount;
float4 _WaterDarkColor;
float4 _WaterBrightColor;
float4 _ShoreFoamColor;
float4 _WaterFoamColor;
float _WaterFoamSpeed;
float _WaterFoamAmount;
float _WaveIntensity;
float _ShoreFoamSpeed;
float _Emission;
float _Scale;
float _BasedOnWorldScale;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
Hash_Tchou_2_2_float(UV, UV);
return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}

void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
float2 g = floor(UV * CellDensity);
float2 f = frac(UV * CellDensity);
float t = 8.0;
float3 res = float3(8.0, 0.0, 0.0);
for (int y = -1; y <= 1; y++)
{
for (int x = -1; x <= 1; x++)
{
float2 lattice = float2(x, y);
float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
float d = distance(lattice + offset, f);
if (d < res.x)
{
res = float3(d, offset.x, offset.y);
Out = res.x;
Cells = res.y;
}
}
}
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
{
float2 i = floor(uv);
float2 f = frac(uv);
f = f * f * (3.0 - 2.0 * f);
uv = abs(frac(uv) - 0.5);
float2 c0 = i + float2(0.0, 0.0);
float2 c1 = i + float2(1.0, 0.0);
float2 c2 = i + float2(0.0, 1.0);
float2 c3 = i + float2(1.0, 1.0);
float r0; Hash_Tchou_2_1_float(c0, r0);
float r1; Hash_Tchou_2_1_float(c1, r1);
float r2; Hash_Tchou_2_1_float(c2, r2);
float r3; Hash_Tchou_2_1_float(c3, r3);
float bottomOfGrid = lerp(r0, r1, f.x);
float topOfGrid = lerp(r2, r3, f.x);
float t = lerp(bottomOfGrid, topOfGrid, f.y);
return t;
}

void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
{
float freq, amp;
Out = 0.0f;
freq = pow(2.0, float(0));
amp = pow(0.5, float(3-0));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(1));
amp = pow(0.5, float(3-1));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(2));
amp = pow(0.5, float(3-2));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_A_4_Float = 0;
float _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float);
float _Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean = _BasedOnWorldScale;
float _Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float = _Scale;
float3 _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3;
Unity_Branch_float3(_Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float.xxx), _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3);
float3 _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3, float3(6, 6, 6), _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3);
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float;
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float, (_Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3).x, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float);
float _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float;
Unity_OneMinus_float(_Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float);
float _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float;
Unity_Clamp_float(_OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float, float(0), float(1), _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float);
float _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float;
Unity_Subtract_float(_Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float, float(0.5), _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float);
float _Property_af5407b060274092886eaf4639817c3c_Out_0_Float = _WaveIntensity;
float _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float;
Unity_Multiply_float_float(_Property_af5407b060274092886eaf4639817c3c_Out_0_Float, IN.TimeParameters.y, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float);
float _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float;
Unity_Multiply_float_float(_Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float);
float _Add_8149863b52284f3fbe288315639f5849_Out_2_Float;
Unity_Add_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float);
float4 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4;
float3 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
float2 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2;
Unity_Combine_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float, _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float, float(0), _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2);
description.Position = _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float Alpha;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4 = _WaterFoamColor;
float4 _Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4 = _WaterDarkColor;
float4 _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4 = _WaterBrightColor;
float _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float);
float _Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean = _BasedOnWorldScale;
float _Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float = _Scale;
float3 _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3;
Unity_Branch_float3(_Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float.xxx), _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3);
float3 _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3, float3(6, 6, 6), _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3);
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float;
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float, (_Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3).x, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float);
float _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float;
Unity_OneMinus_float(_Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float);
float _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float;
Unity_Add_float(_OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float, float(0), _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float);
float4 _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4;
Unity_Lerp_float4(_Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4, _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4, (_Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float.xxxx), _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4);
float _Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float = _WaterFoamAmount;
float _Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean = _BasedOnWorldScale;
float _Property_55986d71a90a4e388241f53331a78d58_Out_0_Float = _Scale;
float3 _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3;
Unity_Branch_float3(_Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_55986d71a90a4e388241f53331a78d58_Out_0_Float.xxx), _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3);
float3 _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3, float3(1, 1, 1), _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3);
float _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float = _WaterFoamSpeed;
float _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float, _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float);
float2 _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3.xy), (_Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float.xx), _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2);
float _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2, float(200), _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float);
float _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float;
Unity_Step_float(_Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float, _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float, _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float);
float4 _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4;
Unity_Lerp_float4(_Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4, _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4, (_Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float.xxxx), _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4);
float4 _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4 = _ShoreFoamColor;
float _Float_5aaea56370c94e908d531e7748575723_Out_0_Float = float(100);
float _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float;
Unity_Divide_float(_Float_5aaea56370c94e908d531e7748575723_Out_0_Float, float(20), _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float);
float _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float);
float4 _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4 = IN.ScreenPosition;
float _Split_195f72821d3a408c9a6d7c0280c12eb8_R_1_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[0];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_G_2_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[1];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_B_3_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[2];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[3];
float _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float;
Unity_Subtract_float(_SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float, _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float, _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float);
float _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float;
Unity_Saturate_float(_Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float, _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float);
float _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float;
Unity_OneMinus_float(_Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float, _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float);
float _Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float = _ShoreFoamAmount;
float _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
Unity_OneMinus_float(_Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float, _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float);
float _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float = _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
float _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float;
Unity_Subtract_float(_OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float, _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float, _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float);
float _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float;
Unity_Clamp_float(_Subtract_e65156af410049729091e8151a177cb1_Out_2_Float, float(0), float(1), _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float);
float _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float;
Unity_Multiply_float_float(_Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float, _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float, _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float);
float _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float = _ShoreFoamSpeed;
float _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float, _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float);
float2 _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float.xx), _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2);
float _Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean = _BasedOnWorldScale;
float _Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float = _Scale;
float3 _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3;
Unity_Branch_float3(_Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float.xxx), _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3);
float3 _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3, float3(300, 2, 2), _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3);
float _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2, (_Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3).x, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float);
float _Add_4e789725a9d5427e86993028f873799e_Out_2_Float;
Unity_Add_float(_Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float, _Add_4e789725a9d5427e86993028f873799e_Out_2_Float);
float _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float;
Unity_Step_float(float(0.81), _Add_4e789725a9d5427e86993028f873799e_Out_2_Float, _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float);
float4 _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4, _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4, (_Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float.xxxx), _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4);
float _Split_3508d8cecb3a4376858da493969c0a7e_R_1_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[0];
float _Split_3508d8cecb3a4376858da493969c0a7e_G_2_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[1];
float _Split_3508d8cecb3a4376858da493969c0a7e_B_3_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[2];
float _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[3];
surface.Alpha = _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpacePosition =                        input.positionOS;
    output.uv0 =                                        input.uv0;
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    





    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/Upstream/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/Upstream/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "DepthNormals"
    Tags
    {
        "LightMode" = "DepthNormals"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHNORMALS
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 tangentWS : INTERP0;
 float4 texCoord0 : INTERP1;
 float3 positionWS : INTERP2;
 float3 normalWS : INTERP3;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float _ShoreFoamAmount;
float4 _WaterDarkColor;
float4 _WaterBrightColor;
float4 _ShoreFoamColor;
float4 _WaterFoamColor;
float _WaterFoamSpeed;
float _WaterFoamAmount;
float _WaveIntensity;
float _ShoreFoamSpeed;
float _Emission;
float _Scale;
float _BasedOnWorldScale;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
Hash_Tchou_2_2_float(UV, UV);
return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}

void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
float2 g = floor(UV * CellDensity);
float2 f = frac(UV * CellDensity);
float t = 8.0;
float3 res = float3(8.0, 0.0, 0.0);
for (int y = -1; y <= 1; y++)
{
for (int x = -1; x <= 1; x++)
{
float2 lattice = float2(x, y);
float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
float d = distance(lattice + offset, f);
if (d < res.x)
{
res = float3(d, offset.x, offset.y);
Out = res.x;
Cells = res.y;
}
}
}
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
{
float2 i = floor(uv);
float2 f = frac(uv);
f = f * f * (3.0 - 2.0 * f);
uv = abs(frac(uv) - 0.5);
float2 c0 = i + float2(0.0, 0.0);
float2 c1 = i + float2(1.0, 0.0);
float2 c2 = i + float2(0.0, 1.0);
float2 c3 = i + float2(1.0, 1.0);
float r0; Hash_Tchou_2_1_float(c0, r0);
float r1; Hash_Tchou_2_1_float(c1, r1);
float r2; Hash_Tchou_2_1_float(c2, r2);
float r3; Hash_Tchou_2_1_float(c3, r3);
float bottomOfGrid = lerp(r0, r1, f.x);
float topOfGrid = lerp(r2, r3, f.x);
float t = lerp(bottomOfGrid, topOfGrid, f.y);
return t;
}

void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
{
float freq, amp;
Out = 0.0f;
freq = pow(2.0, float(0));
amp = pow(0.5, float(3-0));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(1));
amp = pow(0.5, float(3-1));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(2));
amp = pow(0.5, float(3-2));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_A_4_Float = 0;
float _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float);
float _Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean = _BasedOnWorldScale;
float _Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float = _Scale;
float3 _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3;
Unity_Branch_float3(_Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float.xxx), _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3);
float3 _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3, float3(6, 6, 6), _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3);
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float;
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float, (_Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3).x, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float);
float _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float;
Unity_OneMinus_float(_Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float);
float _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float;
Unity_Clamp_float(_OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float, float(0), float(1), _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float);
float _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float;
Unity_Subtract_float(_Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float, float(0.5), _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float);
float _Property_af5407b060274092886eaf4639817c3c_Out_0_Float = _WaveIntensity;
float _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float;
Unity_Multiply_float_float(_Property_af5407b060274092886eaf4639817c3c_Out_0_Float, IN.TimeParameters.y, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float);
float _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float;
Unity_Multiply_float_float(_Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float);
float _Add_8149863b52284f3fbe288315639f5849_Out_2_Float;
Unity_Add_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float);
float4 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4;
float3 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
float2 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2;
Unity_Combine_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float, _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float, float(0), _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2);
description.Position = _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 NormalTS;
float Alpha;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4 = _WaterFoamColor;
float4 _Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4 = _WaterDarkColor;
float4 _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4 = _WaterBrightColor;
float _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float);
float _Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean = _BasedOnWorldScale;
float _Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float = _Scale;
float3 _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3;
Unity_Branch_float3(_Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float.xxx), _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3);
float3 _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3, float3(6, 6, 6), _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3);
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float;
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float, (_Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3).x, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float);
float _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float;
Unity_OneMinus_float(_Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float);
float _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float;
Unity_Add_float(_OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float, float(0), _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float);
float4 _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4;
Unity_Lerp_float4(_Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4, _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4, (_Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float.xxxx), _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4);
float _Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float = _WaterFoamAmount;
float _Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean = _BasedOnWorldScale;
float _Property_55986d71a90a4e388241f53331a78d58_Out_0_Float = _Scale;
float3 _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3;
Unity_Branch_float3(_Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_55986d71a90a4e388241f53331a78d58_Out_0_Float.xxx), _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3);
float3 _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3, float3(1, 1, 1), _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3);
float _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float = _WaterFoamSpeed;
float _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float, _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float);
float2 _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3.xy), (_Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float.xx), _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2);
float _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2, float(200), _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float);
float _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float;
Unity_Step_float(_Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float, _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float, _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float);
float4 _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4;
Unity_Lerp_float4(_Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4, _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4, (_Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float.xxxx), _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4);
float4 _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4 = _ShoreFoamColor;
float _Float_5aaea56370c94e908d531e7748575723_Out_0_Float = float(100);
float _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float;
Unity_Divide_float(_Float_5aaea56370c94e908d531e7748575723_Out_0_Float, float(20), _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float);
float _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float);
float4 _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4 = IN.ScreenPosition;
float _Split_195f72821d3a408c9a6d7c0280c12eb8_R_1_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[0];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_G_2_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[1];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_B_3_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[2];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[3];
float _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float;
Unity_Subtract_float(_SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float, _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float, _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float);
float _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float;
Unity_Saturate_float(_Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float, _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float);
float _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float;
Unity_OneMinus_float(_Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float, _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float);
float _Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float = _ShoreFoamAmount;
float _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
Unity_OneMinus_float(_Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float, _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float);
float _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float = _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
float _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float;
Unity_Subtract_float(_OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float, _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float, _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float);
float _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float;
Unity_Clamp_float(_Subtract_e65156af410049729091e8151a177cb1_Out_2_Float, float(0), float(1), _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float);
float _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float;
Unity_Multiply_float_float(_Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float, _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float, _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float);
float _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float = _ShoreFoamSpeed;
float _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float, _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float);
float2 _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float.xx), _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2);
float _Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean = _BasedOnWorldScale;
float _Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float = _Scale;
float3 _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3;
Unity_Branch_float3(_Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float.xxx), _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3);
float3 _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3, float3(300, 2, 2), _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3);
float _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2, (_Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3).x, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float);
float _Add_4e789725a9d5427e86993028f873799e_Out_2_Float;
Unity_Add_float(_Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float, _Add_4e789725a9d5427e86993028f873799e_Out_2_Float);
float _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float;
Unity_Step_float(float(0.81), _Add_4e789725a9d5427e86993028f873799e_Out_2_Float, _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float);
float4 _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4, _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4, (_Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float.xxxx), _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4);
float _Split_3508d8cecb3a4376858da493969c0a7e_R_1_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[0];
float _Split_3508d8cecb3a4376858da493969c0a7e_G_2_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[1];
float _Split_3508d8cecb3a4376858da493969c0a7e_B_3_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[2];
float _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[3];
surface.NormalTS = IN.TangentSpaceNormal;
surface.Alpha = _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.uv0 =                                        input.uv0;
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    



    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/Upstream/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Meta"
    Tags
    {
        "LightMode" = "Meta"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature _ EDITOR_VISUALIZATION
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_INSTANCEID
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_META
#define _FOG_FRAGMENT 1
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float4 texCoord0;
 float4 texCoord1;
 float4 texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 texCoord1 : INTERP1;
 float4 texCoord2 : INTERP2;
 float3 positionWS : INTERP3;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.positionWS.xyz = input.positionWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.positionWS = input.positionWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float _ShoreFoamAmount;
float4 _WaterDarkColor;
float4 _WaterBrightColor;
float4 _ShoreFoamColor;
float4 _WaterFoamColor;
float _WaterFoamSpeed;
float _WaterFoamAmount;
float _WaveIntensity;
float _ShoreFoamSpeed;
float _Emission;
float _Scale;
float _BasedOnWorldScale;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
Hash_Tchou_2_2_float(UV, UV);
return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}

void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
float2 g = floor(UV * CellDensity);
float2 f = frac(UV * CellDensity);
float t = 8.0;
float3 res = float3(8.0, 0.0, 0.0);
for (int y = -1; y <= 1; y++)
{
for (int x = -1; x <= 1; x++)
{
float2 lattice = float2(x, y);
float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
float d = distance(lattice + offset, f);
if (d < res.x)
{
res = float3(d, offset.x, offset.y);
Out = res.x;
Cells = res.y;
}
}
}
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
{
float2 i = floor(uv);
float2 f = frac(uv);
f = f * f * (3.0 - 2.0 * f);
uv = abs(frac(uv) - 0.5);
float2 c0 = i + float2(0.0, 0.0);
float2 c1 = i + float2(1.0, 0.0);
float2 c2 = i + float2(0.0, 1.0);
float2 c3 = i + float2(1.0, 1.0);
float r0; Hash_Tchou_2_1_float(c0, r0);
float r1; Hash_Tchou_2_1_float(c1, r1);
float r2; Hash_Tchou_2_1_float(c2, r2);
float r3; Hash_Tchou_2_1_float(c3, r3);
float bottomOfGrid = lerp(r0, r1, f.x);
float topOfGrid = lerp(r2, r3, f.x);
float t = lerp(bottomOfGrid, topOfGrid, f.y);
return t;
}

void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
{
float freq, amp;
Out = 0.0f;
freq = pow(2.0, float(0));
amp = pow(0.5, float(3-0));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(1));
amp = pow(0.5, float(3-1));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(2));
amp = pow(0.5, float(3-2));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_A_4_Float = 0;
float _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float);
float _Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean = _BasedOnWorldScale;
float _Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float = _Scale;
float3 _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3;
Unity_Branch_float3(_Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float.xxx), _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3);
float3 _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3, float3(6, 6, 6), _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3);
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float;
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float, (_Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3).x, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float);
float _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float;
Unity_OneMinus_float(_Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float);
float _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float;
Unity_Clamp_float(_OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float, float(0), float(1), _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float);
float _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float;
Unity_Subtract_float(_Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float, float(0.5), _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float);
float _Property_af5407b060274092886eaf4639817c3c_Out_0_Float = _WaveIntensity;
float _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float;
Unity_Multiply_float_float(_Property_af5407b060274092886eaf4639817c3c_Out_0_Float, IN.TimeParameters.y, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float);
float _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float;
Unity_Multiply_float_float(_Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float);
float _Add_8149863b52284f3fbe288315639f5849_Out_2_Float;
Unity_Add_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float);
float4 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4;
float3 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
float2 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2;
Unity_Combine_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float, _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float, float(0), _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2);
description.Position = _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 Emission;
float Alpha;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4 = _WaterFoamColor;
float4 _Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4 = _WaterDarkColor;
float4 _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4 = _WaterBrightColor;
float _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float);
float _Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean = _BasedOnWorldScale;
float _Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float = _Scale;
float3 _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3;
Unity_Branch_float3(_Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float.xxx), _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3);
float3 _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3, float3(6, 6, 6), _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3);
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float;
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float, (_Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3).x, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float);
float _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float;
Unity_OneMinus_float(_Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float);
float _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float;
Unity_Add_float(_OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float, float(0), _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float);
float4 _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4;
Unity_Lerp_float4(_Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4, _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4, (_Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float.xxxx), _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4);
float _Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float = _WaterFoamAmount;
float _Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean = _BasedOnWorldScale;
float _Property_55986d71a90a4e388241f53331a78d58_Out_0_Float = _Scale;
float3 _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3;
Unity_Branch_float3(_Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_55986d71a90a4e388241f53331a78d58_Out_0_Float.xxx), _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3);
float3 _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3, float3(1, 1, 1), _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3);
float _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float = _WaterFoamSpeed;
float _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float, _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float);
float2 _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3.xy), (_Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float.xx), _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2);
float _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2, float(200), _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float);
float _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float;
Unity_Step_float(_Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float, _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float, _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float);
float4 _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4;
Unity_Lerp_float4(_Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4, _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4, (_Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float.xxxx), _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4);
float4 _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4 = _ShoreFoamColor;
float _Float_5aaea56370c94e908d531e7748575723_Out_0_Float = float(100);
float _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float;
Unity_Divide_float(_Float_5aaea56370c94e908d531e7748575723_Out_0_Float, float(20), _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float);
float _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float);
float4 _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4 = IN.ScreenPosition;
float _Split_195f72821d3a408c9a6d7c0280c12eb8_R_1_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[0];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_G_2_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[1];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_B_3_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[2];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[3];
float _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float;
Unity_Subtract_float(_SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float, _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float, _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float);
float _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float;
Unity_Saturate_float(_Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float, _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float);
float _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float;
Unity_OneMinus_float(_Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float, _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float);
float _Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float = _ShoreFoamAmount;
float _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
Unity_OneMinus_float(_Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float, _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float);
float _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float = _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
float _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float;
Unity_Subtract_float(_OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float, _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float, _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float);
float _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float;
Unity_Clamp_float(_Subtract_e65156af410049729091e8151a177cb1_Out_2_Float, float(0), float(1), _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float);
float _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float;
Unity_Multiply_float_float(_Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float, _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float, _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float);
float _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float = _ShoreFoamSpeed;
float _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float, _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float);
float2 _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float.xx), _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2);
float _Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean = _BasedOnWorldScale;
float _Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float = _Scale;
float3 _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3;
Unity_Branch_float3(_Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float.xxx), _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3);
float3 _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3, float3(300, 2, 2), _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3);
float _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2, (_Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3).x, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float);
float _Add_4e789725a9d5427e86993028f873799e_Out_2_Float;
Unity_Add_float(_Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float, _Add_4e789725a9d5427e86993028f873799e_Out_2_Float);
float _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float;
Unity_Step_float(float(0.81), _Add_4e789725a9d5427e86993028f873799e_Out_2_Float, _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float);
float4 _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4, _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4, (_Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float.xxxx), _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4);
float _Property_a41f89ba0a404d258a7307cb564b29f2_Out_0_Float = _Emission;
float4 _Multiply_a0863f588ffb43aab35fdd05439043c6_Out_2_Vector4;
Unity_Multiply_float4_float4(_Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4, (_Property_a41f89ba0a404d258a7307cb564b29f2_Out_0_Float.xxxx), _Multiply_a0863f588ffb43aab35fdd05439043c6_Out_2_Vector4);
float _Split_3508d8cecb3a4376858da493969c0a7e_R_1_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[0];
float _Split_3508d8cecb3a4376858da493969c0a7e_G_2_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[1];
float _Split_3508d8cecb3a4376858da493969c0a7e_B_3_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[2];
float _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[3];
surface.BaseColor = (_Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4.xyz);
surface.Emission = (_Multiply_a0863f588ffb43aab35fdd05439043c6_Out_2_Vector4.xyz);
surface.Alpha = _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.uv0 =                                        input.uv0;
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    





    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/Upstream/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "SceneSelectionPass"
    Tags
    {
        "LightMode" = "SceneSelectionPass"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENESELECTIONPASS 1
#define ALPHA_CLIP_THRESHOLD 1
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.positionWS.xyz = input.positionWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.positionWS = input.positionWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float _ShoreFoamAmount;
float4 _WaterDarkColor;
float4 _WaterBrightColor;
float4 _ShoreFoamColor;
float4 _WaterFoamColor;
float _WaterFoamSpeed;
float _WaterFoamAmount;
float _WaveIntensity;
float _ShoreFoamSpeed;
float _Emission;
float _Scale;
float _BasedOnWorldScale;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
Hash_Tchou_2_2_float(UV, UV);
return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}

void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
float2 g = floor(UV * CellDensity);
float2 f = frac(UV * CellDensity);
float t = 8.0;
float3 res = float3(8.0, 0.0, 0.0);
for (int y = -1; y <= 1; y++)
{
for (int x = -1; x <= 1; x++)
{
float2 lattice = float2(x, y);
float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
float d = distance(lattice + offset, f);
if (d < res.x)
{
res = float3(d, offset.x, offset.y);
Out = res.x;
Cells = res.y;
}
}
}
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
{
float2 i = floor(uv);
float2 f = frac(uv);
f = f * f * (3.0 - 2.0 * f);
uv = abs(frac(uv) - 0.5);
float2 c0 = i + float2(0.0, 0.0);
float2 c1 = i + float2(1.0, 0.0);
float2 c2 = i + float2(0.0, 1.0);
float2 c3 = i + float2(1.0, 1.0);
float r0; Hash_Tchou_2_1_float(c0, r0);
float r1; Hash_Tchou_2_1_float(c1, r1);
float r2; Hash_Tchou_2_1_float(c2, r2);
float r3; Hash_Tchou_2_1_float(c3, r3);
float bottomOfGrid = lerp(r0, r1, f.x);
float topOfGrid = lerp(r2, r3, f.x);
float t = lerp(bottomOfGrid, topOfGrid, f.y);
return t;
}

void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
{
float freq, amp;
Out = 0.0f;
freq = pow(2.0, float(0));
amp = pow(0.5, float(3-0));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(1));
amp = pow(0.5, float(3-1));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(2));
amp = pow(0.5, float(3-2));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_A_4_Float = 0;
float _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float);
float _Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean = _BasedOnWorldScale;
float _Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float = _Scale;
float3 _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3;
Unity_Branch_float3(_Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float.xxx), _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3);
float3 _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3, float3(6, 6, 6), _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3);
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float;
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float, (_Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3).x, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float);
float _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float;
Unity_OneMinus_float(_Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float);
float _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float;
Unity_Clamp_float(_OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float, float(0), float(1), _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float);
float _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float;
Unity_Subtract_float(_Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float, float(0.5), _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float);
float _Property_af5407b060274092886eaf4639817c3c_Out_0_Float = _WaveIntensity;
float _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float;
Unity_Multiply_float_float(_Property_af5407b060274092886eaf4639817c3c_Out_0_Float, IN.TimeParameters.y, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float);
float _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float;
Unity_Multiply_float_float(_Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float);
float _Add_8149863b52284f3fbe288315639f5849_Out_2_Float;
Unity_Add_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float);
float4 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4;
float3 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
float2 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2;
Unity_Combine_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float, _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float, float(0), _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2);
description.Position = _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float Alpha;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4 = _WaterFoamColor;
float4 _Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4 = _WaterDarkColor;
float4 _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4 = _WaterBrightColor;
float _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float);
float _Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean = _BasedOnWorldScale;
float _Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float = _Scale;
float3 _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3;
Unity_Branch_float3(_Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float.xxx), _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3);
float3 _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3, float3(6, 6, 6), _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3);
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float;
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float, (_Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3).x, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float);
float _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float;
Unity_OneMinus_float(_Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float);
float _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float;
Unity_Add_float(_OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float, float(0), _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float);
float4 _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4;
Unity_Lerp_float4(_Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4, _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4, (_Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float.xxxx), _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4);
float _Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float = _WaterFoamAmount;
float _Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean = _BasedOnWorldScale;
float _Property_55986d71a90a4e388241f53331a78d58_Out_0_Float = _Scale;
float3 _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3;
Unity_Branch_float3(_Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_55986d71a90a4e388241f53331a78d58_Out_0_Float.xxx), _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3);
float3 _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3, float3(1, 1, 1), _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3);
float _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float = _WaterFoamSpeed;
float _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float, _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float);
float2 _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3.xy), (_Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float.xx), _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2);
float _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2, float(200), _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float);
float _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float;
Unity_Step_float(_Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float, _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float, _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float);
float4 _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4;
Unity_Lerp_float4(_Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4, _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4, (_Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float.xxxx), _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4);
float4 _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4 = _ShoreFoamColor;
float _Float_5aaea56370c94e908d531e7748575723_Out_0_Float = float(100);
float _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float;
Unity_Divide_float(_Float_5aaea56370c94e908d531e7748575723_Out_0_Float, float(20), _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float);
float _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float);
float4 _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4 = IN.ScreenPosition;
float _Split_195f72821d3a408c9a6d7c0280c12eb8_R_1_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[0];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_G_2_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[1];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_B_3_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[2];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[3];
float _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float;
Unity_Subtract_float(_SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float, _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float, _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float);
float _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float;
Unity_Saturate_float(_Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float, _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float);
float _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float;
Unity_OneMinus_float(_Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float, _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float);
float _Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float = _ShoreFoamAmount;
float _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
Unity_OneMinus_float(_Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float, _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float);
float _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float = _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
float _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float;
Unity_Subtract_float(_OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float, _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float, _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float);
float _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float;
Unity_Clamp_float(_Subtract_e65156af410049729091e8151a177cb1_Out_2_Float, float(0), float(1), _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float);
float _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float;
Unity_Multiply_float_float(_Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float, _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float, _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float);
float _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float = _ShoreFoamSpeed;
float _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float, _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float);
float2 _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float.xx), _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2);
float _Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean = _BasedOnWorldScale;
float _Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float = _Scale;
float3 _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3;
Unity_Branch_float3(_Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float.xxx), _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3);
float3 _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3, float3(300, 2, 2), _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3);
float _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2, (_Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3).x, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float);
float _Add_4e789725a9d5427e86993028f873799e_Out_2_Float;
Unity_Add_float(_Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float, _Add_4e789725a9d5427e86993028f873799e_Out_2_Float);
float _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float;
Unity_Step_float(float(0.81), _Add_4e789725a9d5427e86993028f873799e_Out_2_Float, _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float);
float4 _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4, _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4, (_Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float.xxxx), _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4);
float _Split_3508d8cecb3a4376858da493969c0a7e_R_1_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[0];
float _Split_3508d8cecb3a4376858da493969c0a7e_G_2_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[1];
float _Split_3508d8cecb3a4376858da493969c0a7e_B_3_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[2];
float _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[3];
surface.Alpha = _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.uv0 =                                        input.uv0;
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    





    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/Upstream/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "ScenePickingPass"
    Tags
    {
        "LightMode" = "Picking"
    }

// Render State
Cull Back

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENEPICKINGPASS 1
#define ALPHA_CLIP_THRESHOLD 1
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.positionWS.xyz = input.positionWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.positionWS = input.positionWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float _ShoreFoamAmount;
float4 _WaterDarkColor;
float4 _WaterBrightColor;
float4 _ShoreFoamColor;
float4 _WaterFoamColor;
float _WaterFoamSpeed;
float _WaterFoamAmount;
float _WaveIntensity;
float _ShoreFoamSpeed;
float _Emission;
float _Scale;
float _BasedOnWorldScale;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
Hash_Tchou_2_2_float(UV, UV);
return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}

void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
float2 g = floor(UV * CellDensity);
float2 f = frac(UV * CellDensity);
float t = 8.0;
float3 res = float3(8.0, 0.0, 0.0);
for (int y = -1; y <= 1; y++)
{
for (int x = -1; x <= 1; x++)
{
float2 lattice = float2(x, y);
float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
float d = distance(lattice + offset, f);
if (d < res.x)
{
res = float3(d, offset.x, offset.y);
Out = res.x;
Cells = res.y;
}
}
}
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
{
float2 i = floor(uv);
float2 f = frac(uv);
f = f * f * (3.0 - 2.0 * f);
uv = abs(frac(uv) - 0.5);
float2 c0 = i + float2(0.0, 0.0);
float2 c1 = i + float2(1.0, 0.0);
float2 c2 = i + float2(0.0, 1.0);
float2 c3 = i + float2(1.0, 1.0);
float r0; Hash_Tchou_2_1_float(c0, r0);
float r1; Hash_Tchou_2_1_float(c1, r1);
float r2; Hash_Tchou_2_1_float(c2, r2);
float r3; Hash_Tchou_2_1_float(c3, r3);
float bottomOfGrid = lerp(r0, r1, f.x);
float topOfGrid = lerp(r2, r3, f.x);
float t = lerp(bottomOfGrid, topOfGrid, f.y);
return t;
}

void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
{
float freq, amp;
Out = 0.0f;
freq = pow(2.0, float(0));
amp = pow(0.5, float(3-0));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(1));
amp = pow(0.5, float(3-1));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(2));
amp = pow(0.5, float(3-2));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_A_4_Float = 0;
float _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float);
float _Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean = _BasedOnWorldScale;
float _Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float = _Scale;
float3 _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3;
Unity_Branch_float3(_Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float.xxx), _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3);
float3 _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3, float3(6, 6, 6), _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3);
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float;
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float, (_Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3).x, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float);
float _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float;
Unity_OneMinus_float(_Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float);
float _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float;
Unity_Clamp_float(_OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float, float(0), float(1), _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float);
float _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float;
Unity_Subtract_float(_Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float, float(0.5), _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float);
float _Property_af5407b060274092886eaf4639817c3c_Out_0_Float = _WaveIntensity;
float _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float;
Unity_Multiply_float_float(_Property_af5407b060274092886eaf4639817c3c_Out_0_Float, IN.TimeParameters.y, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float);
float _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float;
Unity_Multiply_float_float(_Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float);
float _Add_8149863b52284f3fbe288315639f5849_Out_2_Float;
Unity_Add_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float);
float4 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4;
float3 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
float2 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2;
Unity_Combine_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float, _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float, float(0), _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2);
description.Position = _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float Alpha;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4 = _WaterFoamColor;
float4 _Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4 = _WaterDarkColor;
float4 _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4 = _WaterBrightColor;
float _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float);
float _Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean = _BasedOnWorldScale;
float _Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float = _Scale;
float3 _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3;
Unity_Branch_float3(_Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float.xxx), _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3);
float3 _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3, float3(6, 6, 6), _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3);
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float;
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float, (_Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3).x, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float);
float _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float;
Unity_OneMinus_float(_Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float);
float _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float;
Unity_Add_float(_OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float, float(0), _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float);
float4 _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4;
Unity_Lerp_float4(_Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4, _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4, (_Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float.xxxx), _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4);
float _Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float = _WaterFoamAmount;
float _Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean = _BasedOnWorldScale;
float _Property_55986d71a90a4e388241f53331a78d58_Out_0_Float = _Scale;
float3 _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3;
Unity_Branch_float3(_Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_55986d71a90a4e388241f53331a78d58_Out_0_Float.xxx), _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3);
float3 _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3, float3(1, 1, 1), _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3);
float _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float = _WaterFoamSpeed;
float _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float, _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float);
float2 _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3.xy), (_Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float.xx), _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2);
float _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2, float(200), _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float);
float _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float;
Unity_Step_float(_Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float, _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float, _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float);
float4 _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4;
Unity_Lerp_float4(_Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4, _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4, (_Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float.xxxx), _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4);
float4 _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4 = _ShoreFoamColor;
float _Float_5aaea56370c94e908d531e7748575723_Out_0_Float = float(100);
float _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float;
Unity_Divide_float(_Float_5aaea56370c94e908d531e7748575723_Out_0_Float, float(20), _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float);
float _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float);
float4 _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4 = IN.ScreenPosition;
float _Split_195f72821d3a408c9a6d7c0280c12eb8_R_1_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[0];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_G_2_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[1];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_B_3_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[2];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[3];
float _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float;
Unity_Subtract_float(_SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float, _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float, _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float);
float _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float;
Unity_Saturate_float(_Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float, _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float);
float _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float;
Unity_OneMinus_float(_Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float, _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float);
float _Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float = _ShoreFoamAmount;
float _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
Unity_OneMinus_float(_Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float, _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float);
float _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float = _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
float _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float;
Unity_Subtract_float(_OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float, _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float, _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float);
float _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float;
Unity_Clamp_float(_Subtract_e65156af410049729091e8151a177cb1_Out_2_Float, float(0), float(1), _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float);
float _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float;
Unity_Multiply_float_float(_Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float, _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float, _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float);
float _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float = _ShoreFoamSpeed;
float _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float, _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float);
float2 _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float.xx), _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2);
float _Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean = _BasedOnWorldScale;
float _Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float = _Scale;
float3 _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3;
Unity_Branch_float3(_Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float.xxx), _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3);
float3 _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3, float3(300, 2, 2), _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3);
float _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2, (_Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3).x, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float);
float _Add_4e789725a9d5427e86993028f873799e_Out_2_Float;
Unity_Add_float(_Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float, _Add_4e789725a9d5427e86993028f873799e_Out_2_Float);
float _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float;
Unity_Step_float(float(0.81), _Add_4e789725a9d5427e86993028f873799e_Out_2_Float, _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float);
float4 _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4, _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4, (_Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float.xxxx), _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4);
float _Split_3508d8cecb3a4376858da493969c0a7e_R_1_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[0];
float _Split_3508d8cecb3a4376858da493969c0a7e_G_2_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[1];
float _Split_3508d8cecb3a4376858da493969c0a7e_B_3_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[2];
float _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[3];
surface.BaseColor = (_Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4.xyz);
surface.Alpha = _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.uv0 =                                        input.uv0;
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    





    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/Upstream/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Universal 2D"
    Tags
    {
        "LightMode" = "Universal2D"
    }

// Render State
Cull Back
Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
ZTest LEqual
ZWrite Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_2D
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float4 uv0;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 positionWS : INTERP1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.positionWS.xyz = input.positionWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.positionWS = input.positionWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float _ShoreFoamAmount;
float4 _WaterDarkColor;
float4 _WaterBrightColor;
float4 _ShoreFoamColor;
float4 _WaterFoamColor;
float _WaterFoamSpeed;
float _WaterFoamAmount;
float _WaveIntensity;
float _ShoreFoamSpeed;
float _Emission;
float _Scale;
float _BasedOnWorldScale;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties

// Graph Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
Hash_Tchou_2_2_float(UV, UV);
return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}

void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
float2 g = floor(UV * CellDensity);
float2 f = frac(UV * CellDensity);
float t = 8.0;
float3 res = float3(8.0, 0.0, 0.0);
for (int y = -1; y <= 1; y++)
{
for (int x = -1; x <= 1; x++)
{
float2 lattice = float2(x, y);
float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
float d = distance(lattice + offset, f);
if (d < res.x)
{
res = float3(d, offset.x, offset.y);
Out = res.x;
Cells = res.y;
}
}
}
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Clamp_float(float In, float Min, float Max, out float Out)
{
    Out = clamp(In, Min, Max);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
{
float2 i = floor(uv);
float2 f = frac(uv);
f = f * f * (3.0 - 2.0 * f);
uv = abs(frac(uv) - 0.5);
float2 c0 = i + float2(0.0, 0.0);
float2 c1 = i + float2(1.0, 0.0);
float2 c2 = i + float2(0.0, 1.0);
float2 c3 = i + float2(1.0, 1.0);
float r0; Hash_Tchou_2_1_float(c0, r0);
float r1; Hash_Tchou_2_1_float(c1, r1);
float r2; Hash_Tchou_2_1_float(c2, r2);
float r3; Hash_Tchou_2_1_float(c3, r3);
float bottomOfGrid = lerp(r0, r1, f.x);
float topOfGrid = lerp(r2, r3, f.x);
float t = lerp(bottomOfGrid, topOfGrid, f.y);
return t;
}

void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
{
float freq, amp;
Out = 0.0f;
freq = pow(2.0, float(0));
amp = pow(0.5, float(3-0));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(1));
amp = pow(0.5, float(3-1));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(2));
amp = pow(0.5, float(3-2));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
}

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_65bd8e19f75f4a01907c33b1be1a50a0_A_4_Float = 0;
float _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float);
float _Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean = _BasedOnWorldScale;
float _Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float = _Scale;
float3 _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3;
Unity_Branch_float3(_Property_65241134f32c4ab4973a125faf503a1f_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_b11aa428e728492a919d2ff9cafacd43_Out_0_Float.xxx), _Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3);
float3 _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_264a8868e1a74c31a15a516c92347fd5_Out_3_Vector3, float3(6, 6, 6), _Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3);
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float;
float _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_6497776fec3c40c4aca910b602ecdd54_Out_2_Float, (_Multiply_4c0fb9215b704148b360fca9065082dc_Out_2_Vector3).x, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _Voronoi_7eacb8b7173c4c4393385a8a49246814_Cells_4_Float);
float _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float;
Unity_OneMinus_float(_Voronoi_7eacb8b7173c4c4393385a8a49246814_Out_3_Float, _OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float);
float _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float;
Unity_Clamp_float(_OneMinus_b57005c4580d4942ab7934b82d24946d_Out_1_Float, float(0), float(1), _Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float);
float _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float;
Unity_Subtract_float(_Clamp_61e712a946dc4f329ed3984b90ec7d1e_Out_3_Float, float(0.5), _Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float);
float _Property_af5407b060274092886eaf4639817c3c_Out_0_Float = _WaveIntensity;
float _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float;
Unity_Multiply_float_float(_Property_af5407b060274092886eaf4639817c3c_Out_0_Float, IN.TimeParameters.y, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float);
float _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float;
Unity_Multiply_float_float(_Subtract_18575b33e17c4a9ebcea96d194cb1054_Out_2_Float, _Multiply_9bc2b182c3524365ad31e9657be17427_Out_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float);
float _Add_8149863b52284f3fbe288315639f5849_Out_2_Float;
Unity_Add_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_G_2_Float, _Multiply_9c2c24a8f4c248c0a355895705db69b3_Out_2_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float);
float4 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4;
float3 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
float2 _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2;
Unity_Combine_float(_Split_65bd8e19f75f4a01907c33b1be1a50a0_R_1_Float, _Add_8149863b52284f3fbe288315639f5849_Out_2_Float, _Split_65bd8e19f75f4a01907c33b1be1a50a0_B_3_Float, float(0), _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGBA_4_Vector4, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3, _Combine_76bbc1303d664aa2a16a06b69b2fda89_RG_6_Vector2);
description.Position = _Combine_76bbc1303d664aa2a16a06b69b2fda89_RGB_5_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float Alpha;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4 = _WaterFoamColor;
float4 _Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4 = _WaterDarkColor;
float4 _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4 = _WaterBrightColor;
float _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float;
Unity_Divide_float(IN.TimeParameters.x, float(2), _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float);
float _Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean = _BasedOnWorldScale;
float _Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float = _Scale;
float3 _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3;
Unity_Branch_float3(_Property_1eb4c0aad2bc47009f15eaf0fe83754b_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_755ef8c8f57441b28f10c1c84cb6d3be_Out_0_Float.xxx), _Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3);
float3 _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_41d79a1ccc7741d8b4d41b9052bedf22_Out_3_Vector3, float3(6, 6, 6), _Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3);
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float;
float _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float;
Unity_Voronoi_Deterministic_float(IN.uv0.xy, _Divide_1ae0d69e4bb84a36bfe9f57d99923008_Out_2_Float, (_Multiply_bb3084dc29b5484db6be18f1e1ce649b_Out_2_Vector3).x, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _Voronoi_61d3822e1cf74384baad3bdd78a78067_Cells_4_Float);
float _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float;
Unity_OneMinus_float(_Voronoi_61d3822e1cf74384baad3bdd78a78067_Out_3_Float, _OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float);
float _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float;
Unity_Add_float(_OneMinus_93aaebe6f5f844e5b954b931f684af7e_Out_1_Float, float(0), _Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float);
float4 _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4;
Unity_Lerp_float4(_Property_ace4032feef641e1959f9cce8e41c3a4_Out_0_Vector4, _Property_2a6f4abb9aa9455a89499f462b5392d7_Out_0_Vector4, (_Add_664df17e23694ea79efc6c4c63b2d58d_Out_2_Float.xxxx), _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4);
float _Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float = _WaterFoamAmount;
float _Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean = _BasedOnWorldScale;
float _Property_55986d71a90a4e388241f53331a78d58_Out_0_Float = _Scale;
float3 _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3;
Unity_Branch_float3(_Property_5ddd056afa3e4b31a2a936af72cb83e7_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_55986d71a90a4e388241f53331a78d58_Out_0_Float.xxx), _Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3);
float3 _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_effb28571b634ed896ae3398d64f1561_Out_3_Vector3, float3(1, 1, 1), _Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3);
float _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float = _WaterFoamSpeed;
float _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_f316ce4cafc74c39ab2cd1929d8e57b2_Out_0_Float, _Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float);
float2 _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_9f3c99d9735244c09af79c9452659a7e_Out_2_Vector3.xy), (_Multiply_489262f36c5944d186fb27a1fc962f7a_Out_2_Float.xx), _TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2);
float _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_f34920c9976c49ec970ac87f59f7cdb1_Out_3_Vector2, float(200), _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float);
float _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float;
Unity_Step_float(_Property_196223cc6b854ce0ad2777d8db894fdb_Out_0_Float, _SimpleNoise_6a81d5c6df5a4a25a4e881c7c3a0f68a_Out_2_Float, _Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float);
float4 _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4;
Unity_Lerp_float4(_Property_392838496eef4a6b9a7bcd018d88bd2d_Out_0_Vector4, _Lerp_872fa8c0993c4c01ba14de900dfdb8be_Out_3_Vector4, (_Step_77ccea3cfeb744ac9b73088b99c5338c_Out_2_Float.xxxx), _Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4);
float4 _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4 = _ShoreFoamColor;
float _Float_5aaea56370c94e908d531e7748575723_Out_0_Float = float(100);
float _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float;
Unity_Divide_float(_Float_5aaea56370c94e908d531e7748575723_Out_0_Float, float(20), _Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float);
float _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float);
float4 _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4 = IN.ScreenPosition;
float _Split_195f72821d3a408c9a6d7c0280c12eb8_R_1_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[0];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_G_2_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[1];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_B_3_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[2];
float _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float = _ScreenPosition_75c4b23bb82a475783df41d7cedf7e27_Out_0_Vector4[3];
float _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float;
Unity_Subtract_float(_SceneDepth_d7f4ddf86fb34e38896432be257981c3_Out_1_Float, _Split_195f72821d3a408c9a6d7c0280c12eb8_A_4_Float, _Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float);
float _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float;
Unity_Saturate_float(_Subtract_3bc140f1673649c5882c43cf99bd2852_Out_2_Float, _Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float);
float _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float;
Unity_OneMinus_float(_Saturate_8bc37211f8a64153a82eba8826c67587_Out_1_Float, _OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float);
float _Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float = _ShoreFoamAmount;
float _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
Unity_OneMinus_float(_Property_e2eb21f5865e4711aa5831a75e2377ed_Out_0_Float, _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float);
float _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float = _OneMinus_162dc3c9429340cd827974387706bcd1_Out_1_Float;
float _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float;
Unity_Subtract_float(_OneMinus_26fe9fb678354ece86812c6cbc15ca0c_Out_1_Float, _Float_ca7c6e11eb224ced8e417692e6951580_Out_0_Float, _Subtract_e65156af410049729091e8151a177cb1_Out_2_Float);
float _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float;
Unity_Clamp_float(_Subtract_e65156af410049729091e8151a177cb1_Out_2_Float, float(0), float(1), _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float);
float _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float;
Unity_Multiply_float_float(_Divide_6bae3ef95b514b87a0c97569ee32bf76_Out_2_Float, _Clamp_719b058ce5534bef8b64e5c3b9384a32_Out_3_Float, _Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float);
float _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float = _ShoreFoamSpeed;
float _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Property_fe31fe15ab104372b4f6a8439bebb4be_Out_0_Float, _Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float);
float2 _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1, 1), (_Multiply_ae3f7ac934bb456aab338f1e225a55eb_Out_2_Float.xx), _TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2);
float _Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean = _BasedOnWorldScale;
float _Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float = _Scale;
float3 _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3;
Unity_Branch_float3(_Property_0200c6424b964ad79445a9e09b08c4a8_Out_0_Boolean, float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))), (_Property_7b827be02f204ad4beef9a6e10deca5f_Out_0_Float.xxx), _Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3);
float3 _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_7f22cc6e72394917a9a60d8ccf5b4152_Out_3_Vector3, float3(300, 2, 2), _Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3);
float _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_a53ae630ecdd4bd59fae5b9ffd88d910_Out_3_Vector2, (_Multiply_bbfb8bd65f3f46e4be5f963159b86f3b_Out_2_Vector3).x, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float);
float _Add_4e789725a9d5427e86993028f873799e_Out_2_Float;
Unity_Add_float(_Multiply_8bcb83d87f7240edb751dddb568d1cab_Out_2_Float, _SimpleNoise_30113847bf0645059c3e556a9ee40c88_Out_2_Float, _Add_4e789725a9d5427e86993028f873799e_Out_2_Float);
float _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float;
Unity_Step_float(float(0.81), _Add_4e789725a9d5427e86993028f873799e_Out_2_Float, _Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float);
float4 _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_74cdc012f9284a8eb48312df21121721_Out_3_Vector4, _Property_fb67afcf6fd94fc791317aa7d23453f4_Out_0_Vector4, (_Step_d56379a144ea41c88c6e26ac606c970f_Out_2_Float.xxxx), _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4);
float _Split_3508d8cecb3a4376858da493969c0a7e_R_1_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[0];
float _Split_3508d8cecb3a4376858da493969c0a7e_G_2_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[1];
float _Split_3508d8cecb3a4376858da493969c0a7e_B_3_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[2];
float _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float = _Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4[3];
surface.BaseColor = (_Lerp_d9bdcde21885410f983ec1c0b1ac0c51_Out_3_Vector4.xyz);
surface.Alpha = _Split_3508d8cecb3a4376858da493969c0a7e_A_4_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.uv0 =                                        input.uv0;
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    





    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/Upstream/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
}
CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
FallBack "Hidden/Shader Graph/FallbackError"
}