using System;
namespace UnityEngine
{
    public class RangeAttribute(float min,float max):Attribute { }
    public class Transform { public static implicit operator bool(Transform value)=>value!=null; }
    public static class Mathf
    {
        public const float Infinity=float.PositiveInfinity;
        public static float Clamp01(float n)=>Math.Clamp(n,0,1);
        public static float Pow(float n,float p)=>MathF.Pow(n,p);
        public static float LerpUnclamped(float a,float b,float t)=>a+(b-a)*t;
        public static float Lerp(float a,float b,float t)=>LerpUnclamped(a,b,Clamp01(t));
        public static bool Approximately(float a,float b)=>MathF.Abs(a-b)<=Math.Max(.000001f*Math.Max(MathF.Abs(a),MathF.Abs(b)),float.Epsilon*8);
        public static float SmoothDamp(float a,float b,ref float rate,float duration,float max,float dt)=>throw new NotSupportedException("Benchmark only exercises Lerp/LegacyLerp.");
    }
}
namespace Unity.Netcode
{
    public struct NetworkTime
    {
        public double Time;
        public NetworkTime TimeTicksAgo(int ticks)=>new(){Time=Time-ticks/30d};
    }
}
