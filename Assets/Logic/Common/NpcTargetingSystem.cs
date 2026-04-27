using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using static Unity.Mathematics.math;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial struct NpcTargetingSystem : ISystem
    {
        private CollisionFilter _filter;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            _filter = new()
            {
                BelongsTo = 1 << 6,
                CollidesWith = 1 << 1 | 1 << 2 | 1 << 4
            };
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 1. Поиск цели
            state.Dependency = new NpcTargetingJob
            {
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
                TeamLookup = SystemAPI.GetComponentLookup<Team>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                CollisionFilter = _filter
            }.ScheduleParallel(state.Dependency);

            // 2. Логика миньонов
            state.Dependency = new MinionBrainJob
            {
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
            }.ScheduleParallel(state.Dependency);

            // 3. Логика башен
            state.Dependency = new TowerBrainJob().ScheduleParallel(state.Dependency);
            
            state.Dependency = new ReAggrRequestHandlerJob
            {
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
                TeamLookup = SystemAPI.GetComponentLookup<Team>(true),
                Champions = SystemAPI.GetComponentLookup<ChampTag>(true),
                CollisionFilter = _filter
            }.ScheduleParallel(state.Dependency);

        }
    }

    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    public partial struct NpcTargetingJob : IJobEntity
    {
        [ReadOnly] public CollisionFilter CollisionFilter;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        private void Execute(ref NpcTargetEntity target, in LocalTransform transform, 
            in NpcDetectionRadius detect, in Team team, EnabledRefRW<AggressionTag> aggro)
        {
            // Проверка текущей цели
            if (target.Value != Entity.Null && TransformLookup.HasComponent(target.Value))
                if (distance(transform.Position, TransformLookup[target.Value].Position) <= detect.Value) return;

            target.Value = Entity.Null;
            NativeList<DistanceHit> hits = new(Allocator.Temp);
            
            if (CollisionWorld.OverlapSphere(transform.Position, detect.Value, ref hits, CollisionFilter))
            {
                Entity closest = Entity.Null;
                float minDoc = float.MaxValue;

                foreach (DistanceHit hit in hits)
                {
                    if (!TeamLookup.TryGetComponent(hit.Entity, out Team enemyTeam) 
                        || enemyTeam.Value == team.Value) continue;
                    if (hit.Distance < minDoc) { minDoc = hit.Distance; closest = hit.Entity; }
                }
                target.Value = closest;
            }
            
            aggro.ValueRW = target.Value != Entity.Null;
            hits.Dispose();
        }
    }

    [BurstCompile]
    [WithAll(typeof(NeedPath), typeof(MoveTargetPosition))]
    // ignore polnaya huinya
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    public partial struct MinionBrainJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        
        private const float DISTANCE_THRESHOLD = 0.5f;

        private void Execute(ref NpcTargetEntity target, ref MoveTargetPosition movePos, 
            ref LastTargetPosition lastPos, EnabledRefRW<NeedPath> needPath, EnabledRefRW<InAttackArea> inAttackArea,
            in LocalTransform transform, in NpcAttackRadius attack)
        {
            if (target.Value == Entity.Null || !TransformLookup.HasComponent(target.Value))
            {
                inAttackArea.ValueRW = false;
                return;
            }

            float3 targetPos = TransformLookup[target.Value].Position;
            float d = distance(transform.Position, targetPos);
            bool tooFar = d >= attack.Value;

            if (tooFar)
            {
                float3 idealPos = targetPos;
                // Точкой назначения должен быть враг
                movePos.Value = idealPos;

                if (distance(lastPos.Value, idealPos) > 2f)
                {
                    needPath.ValueRW = true;
                    inAttackArea.ValueRW = false;
                    lastPos.Value = idealPos;
                }
            }
            else inAttackArea.ValueRW = true;
        }
    }

    [BurstCompile]
    [WithNone(typeof(NeedPath), typeof(MoveTargetPosition))]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    public partial struct TowerBrainJob : IJobEntity
    {
        private void Execute(in NpcTargetEntity target, 
            EnabledRefRW<InAttackArea> inAttackArea, 
            EnabledRefRW<AggressionTag> aggro) 
            => inAttackArea.ValueRW = aggro.ValueRO;
    }
    
    
    [BurstCompile]
    public partial struct ReAggrRequestHandlerJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<ChampTag> Champions;
        [ReadOnly] public CollisionFilter CollisionFilter;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;

        private void Execute(ref NpcTargetEntity target, in NpcDetectionRadius detect, in LocalTransform transform, 
            in Team team, EnabledRefRW<ReAggrRequest> reAggrRequest, ReAggrRequest reAggr)
        {
            if (Champions.HasComponent(target.Value))
            {
                reAggrRequest.ValueRW = false;
                return;
            }            
            
            NativeList<DistanceHit> hits = new(Allocator.Temp);
            if (CollisionWorld.OverlapSphere(transform.Position, detect.Value, ref hits, CollisionFilter))
            {
                Entity reAggrEnemy = Entity.Null;

                foreach (DistanceHit hit in hits)
                {
                    if (!TeamLookup.TryGetComponent(hit.Entity, out Team enemyTeam) 
                        || enemyTeam.Value == team.Value) continue;
                    if (hit.Entity == reAggr.Value) reAggrEnemy = hit.Entity;
                    break;
                }
                target.Value = reAggrEnemy;
                reAggrRequest.ValueRW = false;
            }
            
            reAggrRequest.ValueRW = false;
            hits.Dispose();
        }
    }
}