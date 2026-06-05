using System.Runtime.CompilerServices;
using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.float3;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;

namespace ROMA2.Logic.Common.Behaviour
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(RVOSystem))]
    public partial struct MoveSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed, 
                    PathPositionElement, FollowPathProperties, Simulate, PhysicsVelocity>()
                .WithAll<AttackRadius, TargetEntity, MaxHealthPoints, RVOAgent>()
                .WithNone<PathFindingRequest, IncorrectPathProperties>()
                .Build();

            state.RequireForUpdate<GameplayingTag>();
            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new MoveJob().ScheduleParallel(_query, state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
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
            if (followPathProperties.ReachedTheTarget 
                || (pathPositions.IsEmpty || followPathProperties.Index < 0) && !followPathProperties.IsCleanPath)
            {
                velocity.Linear = zero;
                return;
            }

            float distToGoalSq = distancesq(transform.Position.xz, goalPos.Value.xz);
            followPathProperties.ReachedTheTarget = distToGoalSq <= 0.1;
            if (followPathProperties.ReachedTheTarget)
            {
                // Обнуляем, так как за кадр персонаж ещё может двинуться
                velocity.Linear = zero;
                return;
            }
            
            // Логика обычного движения
            float2 finalVXZ = distToGoalSq >= 1 // За 1 клетку до конечной точки сбрасываем накопление
                ? lerp(velocity.Linear.xz, agent.BestVelocity.xz, 
                    clamp(moveSpeed.Value / 100, 0.2f, 0.5f)) 
                : agent.BestVelocity.xz;

            // Логика поворота выполняется ТОЛЬКО для нового пути
            if (followPathProperties.IsNewPath) 
                finalVXZ = CalculateRotation(transform.Rotation, agent.BestVelocity, maxHP.Value, ref followPathProperties);
            
            velocity.Linear = new(finalVXZ.x, velocity.Linear.y, finalVXZ.y); 
            transform.Rotation = LookRotationSafe(velocity.Linear, up());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float2 CalculateRotation(
            quaternion rotation, 
            float3 bestVelocity, 
            int maxHP, 
            ref FollowPathProperties props)
        {
            // Расчет векторов происходит ТОЛЬКО здесь и ТОЛЬКО когда IsNewPath == true
            float3 lookDirection = mul(rotation, new float3(0, 0, 1));
            float3 vBestNormalized = normalizesafe(bestVelocity);

            // Если почти довернули в нужную сторону — сбрасываем флаг нового пути
            if (dot(lookDirection, vBestNormalized) >= 0.95f)
            {
                props.IsNewPath = false;
                return bestVelocity.xz;
            }

            // Чем больше HP персонажа, тем медленнее он разворачивается
            int deltaHP = maxHP / 1000;
            float delta = clamp(2f / (deltaHP * deltaHP), 0.1f, 1f);
            
            return lerp(lookDirection.xz, vBestNormalized.xz, delta);
        }
    }
}
