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
        public int ChampionID;
        public ChampionDatabase Database;
        public Vector3 FirePointOffset;
        public float DetectionRadius = 15;
        public float RVORadius = 0.5f;
        
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
                AddComponent(entity, new MoveTargetPosition { Flag = false });
                AddBuffer<PathPositionElement>(entity);
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
                
                AddComponent(entity, new MaxHealthPoints { Value = config.MaxHealthPoints });
                AddComponent(entity, new CurrentHealthPoints { Value = config.MaxHealthPoints });
                AddComponent(entity, new HealthRegeneration { Value = config.HealthRegeneration });
                AddComponent(entity, new MaxMana { Value = config.MaxMana });
                AddComponent(entity, new ManaRegeneration { Value = config.ManaRegeneration });
                AddComponent<PhysicalPower>(entity, new() { Value = config.PhysicalPower });
                AddComponent<MagicalPower>(entity, new() { Value = config.MagicalPower });
                AddComponent<PhysicalArmor>(entity, new() { Value = config.PhysicalArmor });
                AddComponent<MagicalArmor>(entity, new() { Value = config.MagicalArmor });
                AddComponent(entity, new AttackSpeed { Value = config.AttackSpeed });
                AddComponent(entity, new MoveSpeed { Value = config.MoveSpeed });
            
                AddBuffer<DamageBufferElement>(entity);
                AddBuffer<IncomingDamageChangerElement>(entity);
                AddBuffer<IncomingDamageElement>(entity);
                AddBuffer<DamageThisTick>(entity);
                AddBuffer<ProcessedDamageElement>(entity);

                AddComponent(entity, new AttackRadius { Value = config.AttackRadius });
                if (config.AttackPrefab != null)
                {
                    AddComponent(entity, new RangedAttackProperties
                    {
                        AttackPrefab = GetEntity(config.AttackPrefab, TransformUsageFlags.Dynamic),
                        FirePointOffset = authoring.FirePointOffset
                    });
                }
            
                AddComponent<TargetEntity>(entity, new() { InAttackArea = false });
                AddBuffer<AttackCooldown>(entity);
                AddComponent(entity, new DetectionRadius { Value = authoring.DetectionRadius });
                AddComponent<InAttackArea>(entity);
                SetComponentEnabled<InAttackArea>(entity, false);
            
                AddComponent<RVOAgent>(entity, new() { BodyRadius = authoring.RVORadius });
            }
        }
    }
}