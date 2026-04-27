using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Physics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct MoveSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed,
                    PathPositionElement, FollowPathIndex, Simulate, 
                    PhysicsVelocity>()
                .WithAll<NpcAttackRadius, NpcTargetEntity>()
                .WithNone<NeedPath, MinionTag>()
                .Build();
            
            state.RequireForUpdate<GameplayingTag>();
            state.RequireForUpdate(_query);
        }

        public void OnUpdate(ref SystemState state)
        {
            MoveJob moveJob = new()
            {
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
            };
            state.Dependency = moveJob.ScheduleParallel(_query, state.Dependency);
        }       
    }

    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        private void Execute(
            ref LocalTransform transform,
            ref PhysicsVelocity velocity,
            in MoveTargetPosition target, 
            in MoveSpeed moveSpeed,
            ref FollowPathIndex followPathIndex, 
            ref DynamicBuffer<PathPositionElement> pathPositions,
            in NpcAttackRadius radius,
            in NpcTargetEntity attackTarget)
        {
            if (pathPositions.IsEmpty) 
            {
                velocity.Linear = float3.zero;
                return;
            }

            if (attackTarget.Value != Entity.Null)
            {
                float3 targetPos = TransformLookup[attackTarget.Value].Position;
                if (distance(targetPos, transform.Position) <= radius.Value)
                {
                    velocity.Linear = float3.zero;
                    return;
                }
            }

            float2 targetInt2;
            float2 futurePos;
            int currentIndex = followPathIndex.Value;

            if (currentIndex == 0)
            {
                targetInt2 = new(target.Value.x, target.Value.z);
                futurePos = float2.zero;
            }
            else if (currentIndex > 0 && currentIndex < pathPositions.Length)
            {
                targetInt2 = pathPositions[currentIndex].Value;
                futurePos = pathPositions[currentIndex - 1].Value;
            }
            else
            {
                velocity.Linear = float3.zero;
                return;
            }
            
            float3 selfPosition = transform.Position;
            float3 targetFloat3 = new(targetInt2.x, selfPosition.y, targetInt2.y);
            float3 vectorToTarget = targetFloat3 - selfPosition;

            float2 segmentDir = normalize(targetInt2 - futurePos);
            float2 agentRel = targetInt2 - selfPosition.xz;
            float progress = dot(agentRel, segmentDir);
            
            if (progress >= 0f)
            {
                followPathIndex.Value -= 1;
                velocity.Linear = float3.zero;
                return;
            }

            float3 moveDirection = normalizesafe(vectorToTarget);
            velocity.Linear = moveDirection * moveSpeed.Value;
            
            if (!moveDirection.Equals(float3.zero)) 
                transform.Rotation = LookRotationSafe(moveDirection, up());
        }
    }
}
