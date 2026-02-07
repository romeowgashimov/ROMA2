using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class HealthPointsAuthoring : MonoBehaviour
    {
        public int MaxHealthPoints;

        public class HealthPointsBaker : Baker<HealthPointsAuthoring>
        {
            public override void Bake(HealthPointsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MaxHealthPoints() { Value = authoring.MaxHealthPoints });
                AddComponent(entity, new CurrentHealthPoints() { Value = authoring.MaxHealthPoints });
                AddBuffer<DamageBufferElement>(entity);
                AddBuffer<DamageThisTick>(entity);
            }
        }
    }
}