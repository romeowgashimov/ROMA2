using Logic.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using static Unity.Mathematics.math;

namespace ROMA2.Logic.Common.Behaviour
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial struct TargetingSystem : ISystem
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
            state.Dependency = new FindingTargetJob
            {
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
                TeamLookup = SystemAPI.GetComponentLookup<Team>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                CollisionFilter = _filter
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency = new MoveableBehaviourJob
            {
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency = new NonMoveableBehaviourJob().ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithNone(typeof(ChampTag))]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    public partial struct FindingTargetJob : IJobEntity
    {
        [ReadOnly] public CollisionFilter CollisionFilter;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        private void Execute(ref TargetEntity target, in LocalTransform transform, 
            in DetectionRadius detect, in Team team)
        {
            // Проверка текущей цели
            if (target.Value != Entity.Null && TransformLookup.HasComponent(target.Value))
                if (distancesq(transform.Position, TransformLookup[target.Value].Position) <= detect.Value * detect.Value) 
                    return;

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
            
            hits.Dispose();
        }
    }

    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    public partial struct MoveableBehaviourJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        private void Execute(ref TargetEntity target, ref MoveTargetPosition movePos, 
            ref LastTargetEntityPosition lastPos, EnabledRefRW<PathFindingRequest> needPath, EnabledRefRW<InAttackArea> inAttackArea,
            in LocalTransform transform, in AttackRadius attack)
        {
            if (target.Value == Entity.Null || !TransformLookup.HasComponent(target.Value))
            {
                inAttackArea.ValueRW = false;
                return;
            }

            float3 targetPos = TransformLookup[target.Value].Position;
            float d = distancesq(transform.Position, targetPos);
            bool tooFar = d >= attack.Value * attack.Value;

            if (tooFar)
            {
                // Точкой назначения должен быть враг
                movePos.Value = targetPos;

                if (distancesq(lastPos.Value, targetPos) > 4f && !needPath.ValueRO) // 2^2
                {
                    needPath.ValueRW = true;
                    inAttackArea.ValueRW = false;
                    lastPos.Value = targetPos;
                }
            }
            else inAttackArea.ValueRW = true;
        }
    }

    [BurstCompile]
    [WithNone(typeof(PathFindingRequest), typeof(MoveTargetPosition))]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    public partial struct NonMoveableBehaviourJob : IJobEntity
    {
        private void Execute(
            in TargetEntity targetEntity, 
            EnabledRefRW<InAttackArea> inAttackArea)
        {
            bool hasEnemy = targetEntity.Value != Entity.Null;
            if (inAttackArea.ValueRO != hasEnemy) inAttackArea.ValueRW = hasEnemy;
        }
    }
}