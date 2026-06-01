using Logic.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.float3;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;

namespace ROMA2.Logic.Common.Movement
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct MoveSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed, PathPositionElement, FollowPathProperties, Simulate, PhysicsVelocity>()
                .WithAll<AttackRadius, TargetEntity, MaxHealthPoints, RVOAgent>()
                .WithNone<PathFindingRequest, IncorrectPathProperties>()
                .Build();

            state.RequireForUpdate<GameplayingTag>();
            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new MoveJob
            {
                TransformLookup = GetComponentLookup<LocalTransform>(true),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(_query, state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        [ReadOnly] [NativeDisableContainerSafetyRestriction] 
        public ComponentLookup<LocalTransform> TransformLookup;
        public float DeltaTime;

        private void Execute(
            ref LocalTransform transform,
            ref PhysicsVelocity velocity,
            ref MoveTargetPosition goalPos,
            in MoveSpeed moveSpeed,
            ref FollowPathProperties followPathProperties,
            ref DynamicBuffer<PathPositionElement> pathPositions,
            in AttackRadius radius,
            in RVOAgent agent,
            in MaxHealthPoints maxHP,
            in TargetEntity attackTarget)
        {
            bool isEmpty = pathPositions.IsEmpty;

            // Остановка преследования и наведение, если дошёл до радиуса атаки
            if (attackTarget.Value != Entity.Null && attackTarget.InAttackArea)
            {
                if (TransformLookup.TryGetComponent(attackTarget.Value, out LocalTransform targetTransform))
                {
                    if (!isEmpty) pathPositions.Clear();
                    velocity.Linear = zero;
                    float3 dir = normalizesafe(targetTransform.Position - transform.Position);
                    quaternion targetRot = LookRotationSafe(new(dir.x, 0, dir.z), up());
                    transform.Rotation = slerp(transform.Rotation, targetRot, DeltaTime * 10f);
                    return;
                }
            }
            
            // Не двигаемся, если некуда или когда уже дошли до конца
            if (followPathProperties.ReachedTheTarget 
                || ((isEmpty || followPathProperties.Index < 0) && !followPathProperties.IsCleanPath))
            {
                velocity.Linear = zero;
                return;
            }

            // Логика передвижения
            float delta;
            float2 finalVXZ;
            bool reached = false;
            // Инерция на полный поворот только если новый путь
            if (followPathProperties.IsNewPath)
            {
                // float3(0, 0, 1) - вектор вперёд, с transform.Forward не работает!!! 
                float3 lookDirection = mul(transform.Rotation, new float3(0, 0, 1));
                float3 vBestNormalized = normalizesafe(agent.BestVelocity);

                if (dot(lookDirection, vBestNormalized) >= 0.95f)
                {
                    followPathProperties.IsNewPath = false;
                    reached = lengthsq(goalPos.Value.xz - transform.Position.xz) < 0.1f;
                    delta = reached ? 1 : 0.2f;
                    finalVXZ = lerp(velocity.Linear.xz, agent.BestVelocity.xz, delta);
                }
                else
                {
                    // Чем жирнее персонаж, тем сложнее ему повернуть
                    int deltaHP = maxHP.Value / 1000;
                    delta = clamp(2f / (deltaHP * deltaHP), 0.1f, 1);
                    finalVXZ = lerp(lookDirection.xz, vBestNormalized.xz, delta);
                }
            }
            else
            {
                reached = lengthsq(goalPos.Value.xz - transform.Position.xz) < 0.1f;
                delta = reached ? 1 : 0.2f;
                finalVXZ = lerp(velocity.Linear.xz, agent.BestVelocity.xz, delta);
            }
            
            float3 finalV = new(finalVXZ.x, velocity.Linear.y, finalVXZ.y);
            transform.Rotation = LookRotationSafe(finalV, up());
            velocity.Linear = finalV;
            
            // Если дошли до конца маршрута
            if (reached) followPathProperties.ReachedTheTarget = true;
        }
    }
}
