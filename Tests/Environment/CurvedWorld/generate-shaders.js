const fs=require('fs'),path=require('path');
const packageFolder=fs.readdirSync('Library/PackageCache').find(p=>p.startsWith('com.unity.render-pipelines.universal@'));
if(!packageFolder)throw Error('Open the project in Unity to resolve its URP package first.');
const source='Library/PackageCache/'+packageFolder;
if(JSON.parse(fs.readFileSync(source+'/package.json','utf8')).version!=='17.4.0')throw Error('Review the curved pass patches before regenerating against another URP version.');
const root='Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders';
const common='#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"\n#include "Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/TankCurvedWorld.hlsl"\n';
const files=['Shaders/Lit.shader','Shaders/LitForwardPass.hlsl','Shaders/LitGBufferPass.hlsl','Shaders/LitDepthNormalsPass.hlsl','Shaders/ShadowCasterPass.hlsl','Shaders/DepthOnlyPass.hlsl','ShaderLibrary/ObjectMotionVectors.hlsl','Shaders/Terrain/TerrainLit.shader','Shaders/Terrain/TerrainLitAdd.shader','Shaders/Terrain/TerrainLitBase.shader','Shaders/Terrain/TerrainLitPasses.hlsl','Shaders/Terrain/TerrainLitDepthNormalsPass.hlsl','Editor/ShaderGraph/Includes/Varyings.hlsl','Editor/ShaderGraph/Includes/MotionVectorPass.hlsl'];
function once(s,a,b){if(!s.includes(a))throw Error('Missing anchor '+a);return s.replace(a,b)}
function deform(pos,n,t){return `\n    // Tank Project: shared world-space bend, after skinning/terrain instancing.\n    float3 tankPositionOS = ${pos};\n`+(n?`    ${n} = TankCurveNormalOS(tankPositionOS, ${n});\n`:'')+(t?`    ${t} = TankCurveTangentOS(tankPositionOS, ${t});\n`:'')+`    ${pos} = TankCurvePositionOS(tankPositionOS);\n`;}
for(const rel of files){let s=fs.readFileSync(path.join(source,rel),'utf8').replace(/\r\n/g,'\n');
 if(/LitForwardPass|LitGBufferPass/.test(rel))s=once(s,'    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);',deform('input.positionOS.xyz','input.normalOS','input.tangentOS.xyz')+'    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);');
 if(rel.endsWith('LitDepthNormalsPass.hlsl')&&!rel.includes('Terrain'))s=once(s,'    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);',deform('input.positionOS.xyz','input.normal','input.tangentOS.xyz')+'    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);');
 if(rel.endsWith('/ShadowCasterPass.hlsl'))s=once(s,'    output.positionCS = GetShadowPositionHClip(input);',deform('input.positionOS.xyz','input.normalOS')+'    output.positionCS = GetShadowPositionHClip(input);');
 if(rel.endsWith('/DepthOnlyPass.hlsl'))s=once(s,'    output.positionCS = TransformObjectToHClip(input.position.xyz);',deform('input.position.xyz')+'    output.positionCS = TransformObjectToHClip(input.position.xyz);');
 if(rel.endsWith('TerrainLitPasses.hlsl')){
 s=once(s,'    TerrainInstancing(v.positionOS, v.normalOS, v.texcoord);','    TerrainInstancing(v.positionOS, v.normalOS, v.texcoord);'+deform('v.positionOS.xyz','v.normalOS'));
 s=once(s,'    TerrainInstancing(v.position, v.normalOS, v.texcoord);','    TerrainInstancing(v.position, v.normalOS, v.texcoord);'+deform('v.position.xyz','v.normalOS'));
 s=once(s,'    TerrainInstancing(v.position, v.normalOS);','    TerrainInstancing(v.position, v.normalOS);'+deform('v.position.xyz','v.normalOS'));
 }
 if(rel.endsWith('TerrainLitDepthNormalsPass.hlsl'))s=once(s,'    TerrainInstancing(v.positionOS, v.normalOS, v.texcoord);','    TerrainInstancing(v.positionOS, v.normalOS, v.texcoord);'+deform('v.positionOS.xyz','v.normalOS'));
 if(rel.endsWith('/Varyings.hlsl')){
 s=once(s,'        float3 positionWS = vertexInput.positionWS;','        float3 positionWS = vertexInput.positionWS;\n        float3 tankOriginalPositionWS = positionWS;\n        #if (SHADERPASS != SHADERPASS_META)\n        positionWS = TankCurvePositionWS(positionWS);\n        #endif');
 s=once(s,'        float3 normalWS = TransformObjectToWorldNormal(input.normalOS);','        float3 normalWS = TransformObjectToWorldNormal(input.normalOS);\n        #if (SHADERPASS != SHADERPASS_META)\n        normalWS = TankCurveNormalWS(tankOriginalPositionWS, normalWS);\n        #endif');
 s=once(s,'            float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);','            float3 tankTangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);\n            #if (SHADERPASS != SHADERPASS_META)\n            tankTangentWS = TankCurveTangentWS(tankOriginalPositionWS, tankTangentWS);\n            #endif\n            float4 tangentWS = float4(tankTangentWS, input.tangentOS.w);');
 }
 if(rel.endsWith('/ObjectMotionVectors.hlsl')){
 s=once(s,'    const VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);','    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);\n    vertexInput.positionWS = TankCurvePositionWS(vertexInput.positionWS);\n    vertexInput.positionCS = TransformWorldToHClip(vertexInput.positionWS);');
 s=s.replaceAll('mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.position))','mul(_NonJitteredViewProjMatrix, float4(TankCurvePositionWS(mul(UNITY_MATRIX_M, input.position).xyz), 1))');
 s=s.replaceAll('mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, prevPos))','mul(_PrevViewProjMatrix, float4(TankCurvePreviousPositionWS(mul(UNITY_PREV_MATRIX_M, prevPos).xyz), 1))');
 }
 if(rel.endsWith('/MotionVectorPass.hlsl'))s=s.replaceAll('mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)))','mul(_PrevViewProjMatrix, float4(TankCurvePreviousPositionWS(mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)).xyz), 1.0f))');
 if(rel.endsWith('.shader')){
 s=s.replace('Shader "Universal Render Pipeline/Lit"','Shader "Tank Project/Curved World/Lit"');
 s=s.replaceAll('Hidden/Universal Render Pipeline/Terrain/Lit (Add Pass)','Hidden/Tank Project/Curved World/Terrain Add');
 s=s.replaceAll('Hidden/Universal Render Pipeline/Terrain/Lit (Base Pass)','Hidden/Tank Project/Curved World/Terrain Base');
 s=s.replace('Shader "Universal Render Pipeline/Terrain/Lit"','Shader "Tank Project/Curved World/Terrain Lit"');
 } else s=common+s;
 for(const mapped of files)s=s.replaceAll('Packages/com.unity.render-pipelines.universal/'+mapped,root+'/Upstream/'+mapped);
 const dest=path.join(root,'Upstream',rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,s);
}
for(const name of ['Water','Rock']){let s=fs.readFileSync('Temp/CurvedWorld/'+name+'.shader','utf8');s=s.replace('Shader "Shader Graphs/'+name+'"','Shader "Tank Project/Curved World/'+name+'"');for(const mapped of files)s=s.replaceAll('Packages/com.unity.render-pipelines.universal/'+mapped,root+'/Upstream/'+mapped);fs.writeFileSync(root+'/'+name+'.shader',s);}
console.log('Generated isolated shader variants and '+files.length+' pinned URP pass files.');
