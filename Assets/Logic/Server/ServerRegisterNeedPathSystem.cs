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
                .WithAll<InputMoveTargetPosition, MoveTargetPosition, NeedPath>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build();

            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //_query.SetChangedVersionFilter(ComponentType.ReadOnly<MoveTargetPosition>());

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
            Entity entity, in InputMoveTargetPosition input, 
            ref MoveTargetPosition register)
        {
            if (register.Flag == input.Flag) return;
            ECB.SetComponent<MoveTargetPosition>(key, entity, new() { Value = input.Value, Flag = input.Flag });
            ECB.SetComponentEnabled<NeedPath>(key, entity, true);
        }
    }
}