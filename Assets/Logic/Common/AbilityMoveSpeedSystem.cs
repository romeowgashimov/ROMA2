using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct AbilityMoveSpeedSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((RefRW<LocalTransform> transform, AbilityMoveSpeed abilityMoveSpeed) in SystemAPI
                         .Query<RefRW<LocalTransform>, AbilityMoveSpeed>()
                         .WithAll<Simulate>()
                         .WithNone<DefaultAttackTarget>())
            {
                float deltaTime = SystemAPI.Time.DeltaTime;
                float3 newPos = transform.ValueRW.Forward() * abilityMoveSpeed.Value * deltaTime;
                transform.ValueRW.Position += newPos;
            }
        }
    }
}