using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;

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
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
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
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();
            
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
                DeltaTime = SystemAPI.Time.DeltaTime,
                ECB = ecb
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

        private void Execute(
            ref TargetEntity target, 
            in LocalTransform transform, 
            in DetectionRadius detect, 
            in Team team)
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
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute(
            [ChunkIndexInQuery] int key,
            ref TargetEntity target, 
            ref MoveTargetPosition movePos, 
            ref LastTargetEntityPosition lastPos, 
            EnabledRefRW<PathFindingRequest> needPath, 
            EnabledRefRW<InAttackArea> inAttackArea,
            in LocalTransform transform, 
            in AttackRadius attack,
            ref DynamicBuffer<PathPositionElement> pathPositions,
            ref FollowPathProperties pathProperties,
            in PhysicsVelocity velocity,
            Entity owner)
        {
            if (target.Value == Entity.Null)
            {
                inAttackArea.ValueRW = false;
                target.InAttackArea = false;
                return;
            }

            if (!TransformLookup.TryGetComponent(target.Value, out LocalTransform targetTransform)) return;
            float3 targetPos = targetTransform.Position;
            if (distancesq(transform.Position, targetPos) >= attack.Value * attack.Value - 1)
            {
                if (distancesq(lastPos.Value, targetPos) > 4f && !needPath.ValueRO) // 2^2
                {
                    movePos.Value = targetPos;
                    needPath.ValueRW = true;
                    inAttackArea.ValueRW = false;
                    target.InAttackArea = false;
                    lastPos.Value = targetPos;
                }
            }
            else
            {
                inAttackArea.ValueRW = true;
                target.InAttackArea = true;
                
                // Остановка преследования и наведение, если дошёл до радиуса атаки
                if (!pathPositions.IsEmpty) pathPositions.Clear();
                if (!pathProperties.ReachedTheTarget) pathProperties.ReachedTheTarget = true;
                
                float3 lookDirection = mul(transform.Rotation, new float3(0, 0, 1));
                float3 dir = normalizesafe(targetTransform.Position - transform.Position);

                // Если смотрим в сторону противника, то не разворачиваемся
                if (dot(lookDirection, dir) > 0.95f) return;
                
                PhysicsVelocity newVelocity = velocity;
                newVelocity.Linear = float3.zero;
                ECB.SetComponent(key, owner, newVelocity);
                
                quaternion targetRot = quaternion.LookRotationSafe(new(dir.x, 0, dir.z), up());
                LocalTransform newTransform = transform;
                newTransform.Rotation = slerp(transform.Rotation, targetRot, DeltaTime * 10f);
                ECB.SetComponent(key, owner, newTransform);
            }
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