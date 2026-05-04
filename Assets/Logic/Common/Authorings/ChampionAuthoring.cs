using Logic.Client;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Logic.Common.Authorings
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
                PathFindingRequest pathFindingRequest = new();
                AddComponent(entity, pathFindingRequest);
                SetComponentEnabled<PathFindingRequest>(entity, false);
                AddComponent(entity, new MoveSpeed { Value = authoring.MoveSpeed });
                AddComponent(entity, new MoveTargetPosition { Flag = false });
                DynamicBuffer<PathPositionElement> pathPositions = AddBuffer<PathPositionElement>(entity);
                pathPositions.Capacity = authoring.PathPositionsCapacity;
                AddComponent<FollowPathIndex>(entity);
                AddComponent<AbilityInput>(entity);
                AddComponent<AimingTag>(entity);
                AddComponent<AimInput>(entity);
                AddComponent<ActivatedAbilitiesCommands>(entity);
                AddComponent<NetworkEntityReference>(entity);
                AddComponent<SelectedEntity>(entity);
                
                AddComponent<LastTargetPosition>(entity);
                
                //Это ЮАЙ, детка, он нужен только тем, кем управляют нАсТоЯщИе ЛюДи, а не АвТОбОтЫ
                AddComponent<LastOutlinedEntity>(entity);
                
                // Запекание базовых атак и агра проходит через NpcAttackAuthoring
            }
        }
    }
}