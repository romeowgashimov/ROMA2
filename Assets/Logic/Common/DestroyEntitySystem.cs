using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

            if (!networkTime.IsFirstTimeFullyPredictingTick) return;
            NetworkTick currentTick = networkTime.ServerTick;
            
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach ((RefRW<LocalTransform> transform, Entity entity) in SystemAPI
                         .Query<RefRW<LocalTransform>>()
                         .WithAll<DestroyEntityTag, Simulate>()
                         .WithEntityAccess())
            {
                if(state.World.IsServer())
                    ecb.DestroyEntity(entity);
                else
                    transform.ValueRW.Position = new(1000f, 0, 1000f);
            }
        }
    }
}