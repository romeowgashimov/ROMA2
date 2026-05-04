using Unity.Entities;
using UnityEngine;

namespace Logic.Common.Authorings
{
    public class BasicAttackTargetAuthoring : MonoBehaviour
    {
        private class BasicAttackTargetBaker : Baker<BasicAttackTargetAuthoring>
        {
            public override void Bake(BasicAttackTargetAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BasicAttackTarget>(entity);
            }
        }
    }
}