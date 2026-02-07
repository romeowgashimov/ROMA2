using Logic.Common;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace Logic.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerRegisterNeedPathSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<MoveTargetPosition, RegisterNeedPathComponent, NeedPath>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build();

            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _query.SetChangedVersionFilter(ComponentType.ReadOnly<MoveTargetPosition>());

            EndSimulationEntityCommandBufferSystem.Singleton ecbSingleton = 
                SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            RegisterNeedPathJob job = new()
            {
                ECB = ecb.AsParallelWriter()
            };
            
            state.Dependency = job.ScheduleParallel(_query, state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct RegisterNeedPathJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([EntityIndexInQuery] int key, 
            Entity entity, in MoveTargetPosition input, 
            ref RegisterNeedPathComponent register)
        {
            if (register.Value == input.Flag) return;
            ECB.SetComponent<RegisterNeedPathComponent>(key, entity, new() { Value = input.Flag });
            ECB.SetComponentEnabled<NeedPath>(key, entity, true);
        }
    }
}