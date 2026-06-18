using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ROMA2.Logic.Client.Data
{
    public class HealthBarUIReference : ICleanupComponentData
    {
        public GameObject Value;
    }

    public struct HealthBarOffset : IComponentData
    {
        public float3 Value;
    }

    public struct UpdatedHP4UI : IComponentData
    {
        public float Current;
        public float Max;
    }
    
    public struct UpdatedMana4UI : IComponentData
    {
        public float Current;
        public float Max;
    }

    public class SkillShotUIReference : ICleanupComponentData
    {
        public GameObject Value;
    }
}