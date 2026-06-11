using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Abilities
{
    public class SkillShotAuthoring : MonoBehaviour
    {
        public float PhysicalDamage;
        public int PhysicalPercentage;
    
        private class SkillShotBaker : Baker<SkillShotAuthoring>
        {
            public override void Bake(SkillShotAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<SkillShotAbility>(entity, new()
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
                AddComponent<Owner>(entity);

                AddComponent<IgnoreRegistrationInGrid>(entity);
            }
        }
    }
}