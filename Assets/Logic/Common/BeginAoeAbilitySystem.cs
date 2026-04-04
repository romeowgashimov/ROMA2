using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct BeginAoeAbilitySystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<AbilityInput, AbilityPrefabs, Team, LocalTransform, 
                    AbilityCooldownTicks, AbilityCooldownTargetTicks, Simulate>()
                .Build();
            
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick) return;
            
            EndPredictedSimulationEntityCommandBufferSystem.Singleton ecbSingleton = 
                SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb =  ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            state.Dependency = new BeginAoeAbilityJob
            {
                ECB = ecb.AsParallelWriter(),
                NetworkTime = netTime,
                IsServer = state.WorldUnmanaged.IsServer()
            }.ScheduleParallel(_query, state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct BeginAoeAbilityJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public NetworkTime NetworkTime;
        public bool IsServer;
        
        private void Execute([EntityIndexInQuery] int key,
            in AbilityInput input, AbilityPrefabs abilityPrefab,
            Team team, LocalTransform transform, AbilityCooldownTicks cooldownTicks,
            DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks)
        {
            if (cooldownTargetTicks.IsOnCooldown(NetworkTime, AbilityType.AoeAbility)) return;
            
            if (input.AoeAbility.IsSet)
            {
                Entity newAoeAbility = ECB.Instantiate(key, abilityPrefab.AoeAbility);
                LocalTransform abilityTransform = LocalTransform.FromPosition(transform.Position);
                ECB.SetComponent(key, newAoeAbility, abilityTransform);
                ECB.SetComponent(key, newAoeAbility, team);

                if (IsServer) return;
                
                cooldownTargetTicks.UpdateCooldown(cooldownTicks, NetworkTime, AbilityType.AoeAbility);
            }
        }
    }
}