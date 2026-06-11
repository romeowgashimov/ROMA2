using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Abilities
{
    public class DeathSphereAuthoring : MonoBehaviour
    {
        public float MagicalDamage;
        public int MagicalPercentage;
    
        private class DeathSphereBaker : Baker<DeathSphereAuthoring>
        {
            public override void Bake(DeathSphereAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DeathSphereAbility>(entity, new()
                {
                    MagicalPercentage = authoring.MagicalPercentage
                });
                AddComponent<DefaultDamage>(entity, new()
                {
                    MagicalDamage = authoring.MagicalDamage
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