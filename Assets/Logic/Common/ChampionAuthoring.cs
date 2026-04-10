using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Logic.Common
{
    public class ChampionAuthoring : MonoBehaviour
    {
        public int PathPositionsCapacity = 100;
        public float MoveSpeed;
        public class ChampionBaker : Baker<ChampionAuthoring>
        {
            public override void Bake(ChampionAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ChampTag>(entity);
                AddComponent<NewChampTag>(entity);
                AddComponent<Team>(entity);
                AddComponent<URPMaterialPropertyBaseColor>(entity);
                AddComponent<InputMoveTargetPosition>(entity);
                NeedPath needPath = new();
                AddComponent(entity, needPath);
                SetComponentEnabled<NeedPath>(entity, false);
                AddComponent(entity, new MoveSpeed { Value = authoring.MoveSpeed });
                AddComponent(entity, new MoveTargetPosition { Flag = false });
                DynamicBuffer<PathPositionElement> pathPositions = AddBuffer<PathPositionElement>(entity);
                pathPositions.Capacity = authoring.PathPositionsCapacity;
                AddComponent<FollowPathIndex>(entity);
                AddComponent<AbilityInput>(entity);
                AddComponent<AimInput>(entity);
                AddComponent<NetworkEntityReference>(entity);
            }
        }
    }
}