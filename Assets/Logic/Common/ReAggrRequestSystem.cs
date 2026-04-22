using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(CalculateFrameDamageSystem))]
    [BurstCompile]
    public partial struct ReAggrRequestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NpcsContainer>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            Entity npcContainer = GetSingletonEntity<NpcsContainer>();
            DynamicBuffer<RedNpcBufferElement> redNpcs = GetBuffer<RedNpcBufferElement>(npcContainer);
            DynamicBuffer<BlueNpcBufferElement> blueNpcs = GetBuffer<BlueNpcBufferElement>(npcContainer);
            
            ComponentLookup<ChampTag> champTags = GetComponentLookup<ChampTag>();
            ComponentLookup<LocalTransform> transforms = GetComponentLookup<LocalTransform>();

            EntityCommandBuffer.ParallelWriter ecb = 
                GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged)
                    .AsParallelWriter();
            
            state.Dependency = new ReAggrRequestJob
            {
                RedNpcs = redNpcs,
                BlueNpcs = blueNpcs,
                Champions = champTags,
                Transforms = transforms,
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct ReAggrRequestJob : IJobEntity
    {
        [ReadOnly] public DynamicBuffer<RedNpcBufferElement> RedNpcs;
        [ReadOnly] public DynamicBuffer<BlueNpcBufferElement> BlueNpcs;
        [ReadOnly] public ComponentLookup<ChampTag> Champions;
        [ReadOnly] public ComponentLookup<LocalTransform> Transforms;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute(
            [ChunkIndexInQuery] int sortKey,
            DynamicBuffer<DamageBufferElement> damageBuffer, 
            Team team, 
            NpcDetectionRadius detectionRadius,
            in LocalTransform transform)
        {
            // Если урон можно получить только от врага, иначе нужна проверка на команды.
            // Переписать на overlapsphere 
            foreach (DamageBufferElement damageElement in damageBuffer)
            {
                Entity enemy = damageElement.DealingDamageEntity;
                if (!Champions.HasComponent(enemy)) continue;
                LocalTransform enemyTransform = Transforms[enemy];
                if (distance(transform.Position, enemyTransform.Position) > detectionRadius.Value) continue;
                switch (team.Value)
                {
                    case TeamType.Red:
                        SendReAggrRequest(RedNpcs, Transforms, transform, 20, 14, sortKey, enemy);
                        break;
                    case TeamType.Blue:
                        SendReAggrRequest(BlueNpcs, Transforms, transform, 20, 14, sortKey, enemy);
                        break;
                }
            }
        }

        private void SendReAggrRequest<T>(
            DynamicBuffer<T> npcs,
            ComponentLookup<LocalTransform> transforms,
            LocalTransform ownTransform,
            int towerRadius,
            int minionRadius,
            int sortKey,
            Entity enemy)
        where T : unmanaged, INpcBufferElement
        {
            foreach (T npc in npcs)
            {
                float3 npcPosition = transforms[npc.Value].Position;
                int radius = npc.IsTower ? towerRadius : minionRadius;
                if (distance(npcPosition, ownTransform.Position) <= radius)
                {
                    ECB.SetComponent(sortKey, npc.Value, new ReAggrRequest { Value = enemy });
                    ECB.SetComponentEnabled<ReAggrRequest>(sortKey, npc.Value, true);
                }
            }
        }
    }
}