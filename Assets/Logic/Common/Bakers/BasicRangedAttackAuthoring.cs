using ROMA2.Logic.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class BasicAttackTargetAuthoring : MonoBehaviour
    {
        private class BasicRangedAttackBaker : Baker<BasicAttackTargetAuthoring>
        {
            public override void Bake(BasicAttackTargetAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BasicAttackTarget>(entity);
            }
        }
    }
}