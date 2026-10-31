#ifndef TANK_CURVED_WORLD_INCLUDED
#define TANK_CURVED_WORLD_INCLUDED

// Scene controller supplies the local gameplay-camera origin, including in shadow passes.
// x=strength, y=flat radius. No per-material or network updates.
float4 _TankCurveOrigin;
float4 _TankCurvePreviousOrigin;
float4 _TankCurveParameters;
float4 _TankCurvePreviousParameters;

void TankCurveSample(float3 positionWS, float3 origin, float2 parameters,
    out float drop, out float2 gradient)
{
    float2 delta = positionWS.xz - origin.xz;
    float radius = length(delta);
    float q = max(0.0, radius - parameters.y);
    float raw = parameters.x * q * q * q / (q + 50.0);
    // Smoothly cap extreme distant displacement at 64 world units for conservative bounds.
    float denominator = 1.0 + raw / 64.0;
    drop = raw / denominator;
    float slope = parameters.x * q * q * (2.0 * q + 150.0)
        / ((q + 50.0) * (q + 50.0) * denominator * denominator);
    gradient = delta * (slope / max(radius, 0.0001));
}

float3 TankCurvePositionWS(float3 positionWS)
{
    if (_TankCurveParameters.x <= 0.0) return positionWS;
    float drop; float2 gradient;
    TankCurveSample(positionWS, _TankCurveOrigin.xyz, _TankCurveParameters.xy, drop, gradient);
    positionWS.y -= drop;
    return positionWS;
}

float3 TankCurvePreviousPositionWS(float3 positionWS)
{
    if (_TankCurvePreviousParameters.x <= 0.0) return positionWS;
    float drop; float2 gradient;
    TankCurveSample(positionWS, _TankCurvePreviousOrigin.xyz, _TankCurvePreviousParameters.xy, drop, gradient);
    positionWS.y -= drop;
    return positionWS;
}

float3 TankCurveNormalWS(float3 positionWS, float3 normalWS)
{
    if (_TankCurveParameters.x <= 0.0) return normalWS;
    float drop; float2 gradient;
    TankCurveSample(positionWS, _TankCurveOrigin.xyz, _TankCurveParameters.xy, drop, gradient);
    if (drop == 0.0) return normalWS;
    return normalize(float3(normalWS.x + gradient.x * normalWS.y,
        normalWS.y, normalWS.z + gradient.y * normalWS.y));
}

float3 TankCurveTangentWS(float3 positionWS, float3 tangentWS)
{
    if (_TankCurveParameters.x <= 0.0) return tangentWS;
    float drop; float2 gradient;
    TankCurveSample(positionWS, _TankCurveOrigin.xyz, _TankCurveParameters.xy, drop, gradient);
    if (drop == 0.0) return tangentWS;
    tangentWS.y -= dot(gradient, tangentWS.xz);
    return normalize(tangentWS);
}

float3 TankCurvePositionOS(float3 positionOS)
{
    if (_TankCurveParameters.x <= 0.0) return positionOS;
    float3 positionWS = TransformObjectToWorld(positionOS);
    if (distance(positionWS.xz, _TankCurveOrigin.xz) <= _TankCurveParameters.y) return positionOS;
    return TransformWorldToObject(TankCurvePositionWS(positionWS));
}

float3 TankCurveNormalOS(float3 positionOS, float3 normalOS)
{
    if (_TankCurveParameters.x <= 0.0) return normalOS;
    float3 positionWS = TransformObjectToWorld(positionOS);
    if (distance(positionWS.xz, _TankCurveOrigin.xz) <= _TankCurveParameters.y) return normalOS;
    return TransformWorldToObjectNormal(TankCurveNormalWS(positionWS, TransformObjectToWorldNormal(normalOS)));
}

float3 TankCurveTangentOS(float3 positionOS, float3 tangentOS)
{
    if (_TankCurveParameters.x <= 0.0) return tangentOS;
    float3 positionWS = TransformObjectToWorld(positionOS);
    if (distance(positionWS.xz, _TankCurveOrigin.xz) <= _TankCurveParameters.y) return tangentOS;
    return TransformWorldToObjectDir(TankCurveTangentWS(positionWS, TransformObjectToWorldDir(tangentOS)));
}
#endif
