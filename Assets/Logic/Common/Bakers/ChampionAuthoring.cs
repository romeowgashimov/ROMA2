using ROMA2.Logic.Common.Abilities;
using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Common.Extensions;
using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class ChampionAuthoring : MonoBehaviour
    {
        public int PathPositionsCapacity = 100;
        public int ChampionID;
        public ChampionDatabase Database;
        public Vector3 FirePointOffset;
        public float DetectionRadius = 15;
        
        public class ChampionBaker : Baker<ChampionAuthoring>
        {
            public override void Bake(ChampionAuthoring authoring)
            {
                ChampionConfigEntry config = authoring.Database.FindConfig(authoring.ChampionID);
                
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ChampTag>(entity);
                AddComponent<NewChampTag>(entity);
                AddComponent<Team>(entity);
                AddComponent<InputMoveTargetPosition>(entity);
                PathFindingRequest pathFindingRequest = new();
                AddComponent(entity, pathFindingRequest);
                SetComponentEnabled<PathFindingRequest>(entity, false);
                AddComponent(entity, new MoveSpeed { Value = config.MoveSpeed });
                AddComponent(entity, new MoveTargetPosition { Flag = false });
                DynamicBuffer<PathPositionElement> pathPositions = AddBuffer<PathPositionElement>(entity);
                pathPositions.Capacity = authoring.PathPositionsCapacity;
                AddComponent<FollowPathProperties>(entity);
                AddComponent<IncorrectPathProperties>(entity);
                SetComponentEnabled<IncorrectPathProperties>(entity, false);
                AddComponent<IgnoreRegistrationInGrid>(entity);

                AddComponent<AbilityInput>(entity);
                AddComponent<AimingTag>(entity);
                AddComponent<AimInput>(entity);
                AddComponent<ActivatedAbilitiesCommands>(entity);
                AddComponent<NetworkEntityReference>(entity);
                AddComponent<SelectedEntity>(entity);
                AddComponent<LastTargetEntityPosition>(entity);
                
                this.BakeHealth(config.HealthPoints);
                this.BakeBehaviour(
                    config.AttackRadius,
                    authoring.DetectionRadius,
                    authoring.FirePointOffset,
                    config.AttackSpeed,
                    true,
                    config.AttackPrefab);
            }
        }
    }
}