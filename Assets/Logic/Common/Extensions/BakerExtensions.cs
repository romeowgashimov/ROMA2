using ROMA2.Logic.Common.Abilities;
using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ROMA2.Logic.Common.Extensions
{
    public static class BakerExtensions
    {
        public static void BakeHealth<T>(
            this Baker<T> baker,
            int maxHealth) 
            where T : MonoBehaviour
        {
            Entity entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            baker.AddComponent(entity, new MaxHealthPoints { Value = maxHealth });
            baker.AddComponent(entity, new CurrentHealthPoints { Value = maxHealth });
            baker.AddBuffer<DamageBufferElement>(entity);
            baker.AddBuffer<DamageThisTick>(entity);
        }

        public static void BakeBehaviour<T>(
            this Baker<T> baker,
            float attackRadius,
            float detectionRadius,
            float3 firePointOffset,
            float attackSpeed,
            bool isChampion,
            GameObject attackPrefab = null,
            float RVORadius = 0.5f)
            where T : MonoBehaviour
        {
            Entity entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            baker.AddComponent(entity, new AttackRadius { Value = attackRadius });
            if (attackPrefab != null)
            {
                baker.AddComponent(entity, new RangedAttackProperties
                {
                    AttackPrefab = baker.GetEntity(attackPrefab, TransformUsageFlags.Dynamic),
                    FirePointOffset = firePointOffset
                });
            }
            
            baker.AddComponent<AttackSpeed>(entity, new() { Value = attackSpeed });
            baker.AddComponent<TargetEntity>(entity, new() { InAttackArea = false });
            baker.AddBuffer<AttackCooldown>(entity);
                
            baker.AddComponent(entity, new DetectionRadius { Value = detectionRadius });
            baker.AddComponent<InAttackArea>(entity);
            baker.SetComponentEnabled<InAttackArea>(entity, false);
                
            if (!isChampion)
            {
                baker.AddComponent<ReAggrRequest>(entity);
                baker.SetComponentEnabled<ReAggrRequest>(entity, false);
            }
            
            baker.AddComponent<RVOAgent>(entity, new() { BodyRadius = RVORadius });
        }

        public static void BakeAbilityCommand<T, V>(
            this Baker<T> baker,
            GameObject commandPrefab)
            where T : MonoBehaviour
            where V : unmanaged, IAbilityCommand
        {
            Entity entity = baker.GetEntity(TransformUsageFlags.None);
            baker.AddComponent<AbilityCommand>(entity);
            baker.AddComponent(entity, new V
            {
                Prefab = baker.GetEntity(commandPrefab, TransformUsageFlags.Dynamic)
            });
            baker.AddComponent<IgnoreRegistrationInGrid>(entity);
        }
    }
}