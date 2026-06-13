using ROMA2.Logic.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class HealthPointAuthoring : MonoBehaviour
    {
        public int MaxHealthPoints;
        public int PhysicalArmor;
        public int MagicalArmor;

        public class HealthPointBaker : Baker<HealthPointAuthoring>
        {
            public override void Bake(HealthPointAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MaxHealthPoints { Value = authoring.MaxHealthPoints });
                AddComponent(entity, new CurrentHealthPoints { Value = authoring.MaxHealthPoints });
                AddComponent<PhysicalArmor>(entity, new() { Value = authoring.PhysicalArmor });
                AddComponent<MagicalArmor>(entity, new() { Value = authoring.MagicalArmor });

                AddBuffer<DamageBufferElement>(entity);
                AddBuffer<IncomingDamageChangerElement>(entity);
                AddBuffer<IncomingDamageElement>(entity);
                AddBuffer<DamageThisTick>(entity);
            }
        }
    }
}