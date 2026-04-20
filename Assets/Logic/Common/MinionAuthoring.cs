using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Logic.Common
{
    public class MinionAuthoring : MonoBehaviour
    {
        public float MoveSpeed;

        public class MinionBaker : Baker<MinionAuthoring>
        {
            public override void Bake(MinionAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MinionTag>(entity);
                AddComponent<NewMinionTag>(entity);
                AddComponent(entity, new MoveSpeed { Value = authoring.MoveSpeed });
                AddComponent<MinionPathIndex>(entity);
                AddComponent<MinionPathPosition>(entity);
                AddComponent<NeedPath>(entity);
                SetComponentEnabled<NeedPath>(entity, false);
                AddComponent<FollowPathIndex>(entity);
                DynamicBuffer<PathPositionElement> pathPositions = AddBuffer<PathPositionElement>(entity);
                pathPositions.Capacity = 120;
                AddComponent(entity, new MoveTargetPosition { Flag = false });
                AddComponent<Team>(entity);
                AddComponent<URPMaterialPropertyBaseColor>(entity);
                
                AddComponent<LastTargetPosition>(entity, new() { Value = 0 });
            }
        }
    }
}