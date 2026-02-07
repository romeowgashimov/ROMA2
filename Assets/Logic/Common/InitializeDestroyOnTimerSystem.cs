using Unity.Entities;
using Unity.NetCode;

namespace Logic.Common
{
    public partial struct InitializeDestroyOnTimerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            int simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
            NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            foreach ((DestroyOnTimer destroyOnTimer, Entity entity) in SystemAPI
                         .Query<DestroyOnTimer>()
                         .WithNone<DestroyAtTick>()
                         .WithEntityAccess())
            {
                uint lifetimeInTicks = (uint)(destroyOnTimer.Value * simulationTickRate);
                NetworkTick targetTick = currentTick;
                targetTick.Add(lifetimeInTicks);
                ecb.AddComponent(entity, new DestroyAtTick { Value = targetTick });
            }
        }
    }
}