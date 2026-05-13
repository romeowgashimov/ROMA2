using Logic.Common;
using ROMA2.Logic.Common.Combat;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ROMA2.Logic.Helpers.Bakers
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
            GameObject attackPrefab = null)
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
            baker.AddComponent<TargetEntity>(entity);
            baker.AddBuffer<AttackCooldown>(entity);
                
            baker.AddComponent(entity, new DetectionRadius { Value = detectionRadius });
            baker.AddComponent<InAttackArea>(entity);
            baker.SetComponentEnabled<InAttackArea>(entity, false);
                
            if (!isChampion)
            {
                baker.AddComponent<ReAggrRequest>(entity);
                baker.SetComponentEnabled<ReAggrRequest>(entity, false);
            }
        }
    }
}