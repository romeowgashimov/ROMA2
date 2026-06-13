using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class TowerAuthoring : MonoBehaviour
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
        public float RVORadius = 0.5f;
        public TeamType Team;

        public class TowerBaker : Baker<TowerAuthoring>
        {
            public override void Bake(TowerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<TowerTag>(entity);
                AddComponent<Team>(entity, new() { Value = authoring.Team });
                
                AddComponent(entity, new MaxHealthPoints { Value = authoring.MaxHealthPoints });
                AddComponent(entity, new CurrentHealthPoints { Value = authoring.MaxHealthPoints });
                AddComponent<PhysicalPower>(entity, new() { Value = authoring.PhysicalPower });
                AddComponent<MagicalPower>(entity, new() { Value = authoring.MagicalPower });
                AddComponent<PhysicalArmor>(entity, new() { Value = authoring.PhysicalArmor });
                AddComponent<MagicalArmor>(entity, new() { Value = authoring.MagicalArmor });
                AddComponent(entity, new AttackSpeed { Value = authoring.AttackSpeed });
                
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