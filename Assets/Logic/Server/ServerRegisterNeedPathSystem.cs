using Logic.Common;
using Unity.Burst;
using Unity.Collections;
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
                .WithAll<MoveTargetPosition, LastProcessedClick, NeedPath>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build();

            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NetworkTime>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _query.SetChangedVersionFilter(ComponentType.ReadOnly<MoveTargetPosition>());

            BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = 
                SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
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
            ref LastProcessedClick lastClick)
        {
            if (input.ClickCount > lastClick.Value)
            {
                lastClick.Value = input.ClickCount;

                ECB.SetComponentEnabled<NeedPath>(key, entity, true);
            }
        }
    }
}