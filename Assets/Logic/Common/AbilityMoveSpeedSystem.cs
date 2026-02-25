using Unity.Burst;
using Unity.Entities;
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
                         .WithAll<Simulate>())
            {
                float deltaTime = SystemAPI.Time.DeltaTime;
                transform.ValueRW.Position += transform.ValueRW.Forward() * abilityMoveSpeed.Value * deltaTime;
            }
        }
    }
}