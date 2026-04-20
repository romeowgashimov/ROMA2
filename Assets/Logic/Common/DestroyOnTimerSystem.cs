using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct DestroyOnTimerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            foreach ((DestroyAtTick destroyAtTick, Entity entity) in SystemAPI
                         .Query<DestroyAtTick>()
                         .WithAll<Simulate>()
                         .WithNone<DestroyEntityTag, DefaultAttackTarget>()
                         .WithEntityAccess())
            {
                if(currentTick.Equals(destroyAtTick.Value) || currentTick.IsNewerThan(destroyAtTick.Value))
                    ecb.AddComponent(entity, new DestroyEntityTag());
            }
        }
    }
}