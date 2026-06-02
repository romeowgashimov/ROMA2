using ROMA2.Logic.Common.DamageCalculator;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace ROMA2.Logic.Common.Behaviour
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(CalculateFrameDamageSystem))]
    public partial struct ReAggrSystem : ISystem
    {
        private CollisionFilter _filter;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            _filter = new()
            {
                BelongsTo = 1 << 6,
                CollidesWith = 1 << 1 | 1 << 2 | 1 << 4
            };
        }
        
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = 
                GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged)
                    .AsParallelWriter();
            
            state.Dependency = new ReAggrRequestSenderJob
            {
                CollisionWorld = GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
                TeamLookup = GetComponentLookup<Team>(true),
                Champions = GetComponentLookup<ChampTag>(true),
                CollisionFilter = _filter,
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency = new ReAggrRequestHandlerJob
            {
                TeamLookup = GetComponentLookup<Team>(true),
                Champions = GetComponentLookup<ChampTag>(true),
                TransformLookup = GetComponentLookup<LocalTransform>(true)
                
            }.ScheduleParallel(state.Dependency);
        }
    }
    
    [BurstCompile]
    [WithNone(typeof(DestroyEntityTag))]
    public partial struct ReAggrRequestSenderJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<ChampTag> Champions;
        [ReadOnly] public CollisionFilter CollisionFilter;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute(
            [ChunkIndexInQuery] int sortKey,
            in DynamicBuffer<DamageBufferElement> damageBuffer, 
            in Team team, 
            in DetectionRadius detect,
            in LocalTransform transform)
        {
            // Если урон можно получить только от врага, иначе нужна проверка на команды.
            foreach (DamageBufferElement damageElement in damageBuffer)
            {
                Entity enemy = damageElement.DealingDamageEntity;
                if (!Champions.HasComponent(enemy)) continue;
                
                NativeList<DistanceHit> hits = new(Allocator.Temp);
                if (CollisionWorld.OverlapSphere(transform.Position, detect.Value, ref hits, CollisionFilter))
                {
                    using NativeList<Entity> allies = new(Allocator.Temp);
                    bool enemyInRadius = false;
                    foreach (DistanceHit hit in hits)
                    {
                        if (TeamLookup.TryGetComponent(hit.Entity, out Team entityTeam) 
                            && entityTeam.Value == team.Value) allies.Add(hit.Entity);
                        if (hit.Entity == enemy) enemyInRadius = true;
                    }

                    if (enemyInRadius)
                        foreach (Entity ally in allies)
                        {
                            if (Champions.HasComponent(ally)) continue;
                            
                            ECB.SetComponent(sortKey, ally, new ReAggrRequest { Target = enemy });
                            ECB.SetComponentEnabled<ReAggrRequest>(sortKey, ally, true);
                        }
                }
                hits.Dispose();
            }
        }
    }
    
    [BurstCompile]
    [WithNone(typeof(DestroyEntityTag))]
    public partial struct ReAggrRequestHandlerJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<ChampTag> Champions;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;

        private void Execute(ref TargetEntity target, in DetectionRadius detect, in LocalTransform transform, 
            in Team team, EnabledRefRW<ReAggrRequest> reAggrRequest, ReAggrRequest reAggr)
        {
            if (Champions.HasComponent(target.Value))
            {
                reAggrRequest.ValueRW = false;
                return;
            }

            Entity targetEntity = reAggr.Target;
            if (TransformLookup.TryGetComponent(targetEntity, out LocalTransform targetTransform) 
                && TeamLookup.TryGetComponent(targetEntity, out Team targetTeam) 
                && targetTeam.Value != team.Value 
                && math.lengthsq(transform.Position - targetTransform.Position) <= detect.Value * detect.Value)
                target.Value = targetEntity;
            
            reAggrRequest.ValueRW = false;
        }
    }
}