using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class MinionAuthoring : MonoBehaviour
    {
        public int MaxHealthPoints;
        public int PhysicalPower;
        public int MagicalPower;
        public int PhysicalArmor;
        public int MagicalArmor;
        public int AttackSpeed;
        public int AttackRadius;
        public int DetectionRadius;
        public float FirePointOffset;
        public GameObject AttackPrefab;
        public int MoveSpeed;
        public float RVORadius = 0.5f;

        public class MinionBaker : Baker<MinionAuthoring>
        {
            public override void Bake(MinionAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MinionTag>(entity);
                AddComponent<NewMinionTag>(entity);
                AddComponent<MinionPathIndex>(entity);
                AddComponent<MinionPathPosition>(entity);
                AddComponent<PathFindingRequest>(entity);
                SetComponentEnabled<PathFindingRequest>(entity, false);
                AddComponent<FollowPathProperties>(entity);
                AddBuffer<PathPositionElement>(entity);
                AddComponent(entity, new MoveTargetPosition { Flag = false });
                AddComponent<IncorrectPathProperties>(entity);
                SetComponentEnabled<IncorrectPathProperties>(entity, false);
                AddComponent<IgnoreRegistrationInGrid>(entity);
                AddComponent<Team>(entity);
                AddComponent<LastTargetEntityPosition>(entity, new() { Value = 0 });

                AddComponent(entity, new MaxHealthPoints { Value = authoring.MaxHealthPoints });
                AddComponent(entity, new CurrentHealthPoints { Value = authoring.MaxHealthPoints });
                AddComponent<PhysicalPower>(entity, new() { Value = authoring.PhysicalPower });
                AddComponent<MagicalPower>(entity, new() { Value = authoring.MagicalPower });
                AddComponent<PhysicalArmor>(entity, new() { Value = authoring.PhysicalArmor });
                AddComponent<MagicalArmor>(entity, new() { Value = authoring.MagicalArmor });
                AddComponent(entity, new AttackSpeed { Value = authoring.AttackSpeed });
                AddComponent(entity, new MoveSpeed { Value = authoring.MoveSpeed });
                
                AddBuffer<DamageBufferElement>(entity);
                AddBuffer<IncomingDamageChangerElement>(entity);
                AddBuffer<IncomingDamageElement>(entity);
                AddBuffer<DamageThisTick>(entity);
                AddBuffer<ProcessedDamageElement>(entity);

                AddComponent(entity, new AttackRadius { Value = authoring.AttackRadius });
                if (authoring.AttackPrefab != null)
                {
                    AddComponent(entity, new RangedAttackProperties
                    {
                        AttackPrefab = GetEntity(authoring.AttackPrefab, TransformUsageFlags.Dynamic),
                        FirePointOffset = authoring.FirePointOffset
                    });
                }
            
                AddComponent<TargetEntity>(entity, new() { InAttackArea = false });
                AddBuffer<AttackCooldown>(entity); 
                AddComponent(entity, new DetectionRadius { Value = authoring.DetectionRadius });
                AddComponent<InAttackArea>(entity);
                SetComponentEnabled<InAttackArea>(entity, false);
                
                AddComponent<ReAggrRequest>(entity);
                SetComponentEnabled<ReAggrRequest>(entity, false);
                
                AddComponent<RVOAgent>(entity, new() { BodyRadius = authoring.RVORadius });
            }
        }
    }
}