using Logic.Common;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
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
                AddComponent<PathFindingRequest>(entity);
                SetComponentEnabled<PathFindingRequest>(entity, false);
                AddComponent<FollowPathProperties>(entity);
                DynamicBuffer<PathPositionElement> pathPositions = AddBuffer<PathPositionElement>(entity);
                pathPositions.Capacity = 120;
                AddComponent(entity, new MoveTargetPosition { Flag = false });
                AddComponent<IncorrectPathProperties>(entity);
                SetComponentEnabled<IncorrectPathProperties>(entity, false);
                
                AddComponent<Team>(entity);
                AddComponent<LastTargetEntityPosition>(entity, new() { Value = 0 });
            }
        }
    }
}