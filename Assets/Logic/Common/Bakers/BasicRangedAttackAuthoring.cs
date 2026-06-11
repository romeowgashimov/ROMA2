using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class BasicRangedAttackAuthoring : MonoBehaviour
    {
        private class BasicRangedAttackBaker : Baker<BasicRangedAttackAuthoring>
        {
            public override void Bake(BasicRangedAttackAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BasicAttackTarget>(entity);
                AddComponent<RangedAttack>(entity);
                AddComponent<TriggerEntityInfo>(entity);
                AddComponent<CombineCharsComponent>(entity);

                AddBuffer<AlreadyDamagedEntity>(entity);
                AddComponent<Owner>(entity);

                AddComponent<IgnoreRegistrationInGrid>(entity);
            }
        }
    }
}