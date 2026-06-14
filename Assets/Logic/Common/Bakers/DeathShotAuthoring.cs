using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Abilities
{
    public class DeathShotAuthoring : MonoBehaviour
    {
        public float PhysicalDamage;
        public int PhysicalPercentage;
    
        private class DeathShotBaker : Baker<DeathShotAuthoring>
        {
            public override void Bake(DeathShotAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DeathShotAbility>(entity, new()
                {
                    PhysicalPercentage = authoring.PhysicalPercentage
                });
                AddComponent<DefaultDamage>(entity, new()
                {
                    PhysicalDamage = authoring.PhysicalDamage
                });
                AddComponent<TriggerEntityInfo>(entity);
                AddComponent<CombineCharsComponent>(entity);

                AddBuffer<AlreadyDamagedEntity>(entity);
                AddBuffer<SendDamageElement>(entity);
                AddComponent<Owner>(entity);
                AddComponent<AbilityIndex>(entity);

                AddComponent<IgnoreRegistrationInGrid>(entity);
            }
        }
    }
}