using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class DefaultAttackTargetAuthoring : MonoBehaviour
    {
        private class DefaultAttackTargetBaker : Baker<DefaultAttackTargetAuthoring>
        {
            public override void Bake(DefaultAttackTargetAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DefaultAttackTarget>(entity);
            }
        }
    }
}