using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct BeginAoeAbilitySystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<AbilityInput, AbilityPrefabs, Team, LocalTransform, Simulate>()
                .Build();
            
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EndPredictedSimulationEntityCommandBufferSystem.Singleton ecbSingleton = 
                SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb =  ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick) return;
            NetworkTick currentTick = networkTime.ServerTick;
            
            BeginAoeAbilityJob job = new() { ECB = ecb.AsParallelWriter() };

            state.Dependency = job.ScheduleParallel(_query, state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct BeginAoeAbilityJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        private void Execute([EntityIndexInQuery] int key, AoeAspect aoe)
        {
            if (aoe.ShouldAttack)
            {
                Entity newAoeAbility = ECB.Instantiate(key, aoe.AbilityPrefab);
                LocalTransform abilityTransform = LocalTransform.FromPosition(aoe.AttackPosition);
                ECB.SetComponent(key, newAoeAbility, abilityTransform);
                ECB.SetComponent(key, newAoeAbility, aoe.Team);
            }
        }
    }
}