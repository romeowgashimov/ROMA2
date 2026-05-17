using Logic.Common;
using Unity.Entities;
using UnityEngine;

#if !UNITY_SERVER
using Unity.Rendering;
#endif


namespace ROMA2.Logic.Helpers.Bakers
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
//#if UNITY_SERVER
                AddComponent<PathFindingRequest>(entity);
                SetComponentEnabled<PathFindingRequest>(entity, false);
//#endif
                AddComponent<FollowPathProperties>(entity);
                DynamicBuffer<PathPositionElement> pathPositions = AddBuffer<PathPositionElement>(entity);
                pathPositions.Capacity = 120;
                AddComponent(entity, new MoveTargetPosition { Flag = false });
                AddComponent<Team>(entity);
                AddComponent<LastTargetEntityPosition>(entity, new() { Value = 0 });
                
#if !UNITY_SERVER
                AddComponent<URPMaterialPropertyBaseColor>(entity);
#endif
            }
        }
    }
}