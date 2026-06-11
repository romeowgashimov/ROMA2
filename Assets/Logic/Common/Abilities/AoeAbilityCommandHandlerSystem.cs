using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Common.Extensions;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace ROMA2.Logic.Common.Abilities
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct AoeAbilityCommandHandlerSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<AoeAbilityCommand>();

            _query = SystemAPI.QueryBuilder()
                .WithAll<AbilityInput, AbilityCommands, AbilityCooldownTicks, Team,
                    LocalTransform, AbilityCooldownTargetTicks, AimInput>()
                .WithAll<AoeAbilityCommand, AimingTag, Simulate, MagicalPower>()
                .WithAllRW<ActivatedAbilitiesCommands>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick) return;

            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            bool isServer = state.WorldUnmanaged.IsServer();
            
            foreach ((AoeAbilityCommand aoeAbilityCommand, AbilityCommand abilityCommand,
                         Entity commandEntity) in SystemAPI
                         .Query<AoeAbilityCommand, AbilityCommand>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                aoeAbilityCommand.Send(
                    abilityCommand,
                    commandEntity,
                    state.EntityManager,
                    ecb);
                
                abilityCommand.DrawAbilityUI<AoeAbilityCommand>(
                    state.EntityManager,
                    ecb,
                    isServer);
            }
            
            state.Dependency = new AoeAbilityCommandJob
            {
                ECB = ecb.AsParallelWriter(),
                NetTime = netTime,
                IsServer = isServer
            }.ScheduleParallel(_query, state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct AoeAbilityCommandJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public NetworkTime NetTime;
        public bool IsServer;

        private void Execute([ChunkIndexInQuery] int key, AbilityInput abilityInput,
            AbilityCooldownTicks abilityCooldownTicks, Team team, 
            RefRW<ActivatedAbilitiesCommands> activatedAbilitiesCommands,
            LocalTransform transform, DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            AimInput aimInput, AoeAbilityCommand aoeAbilityCommand, MagicalPower magicalPower, Entity owner)
        {
            if (!aoeAbilityCommand.IsConfirmParallel(
                    key, ECB, activatedAbilitiesCommands, abilityInput, owner)) return;

            Entity ability = aoeAbilityCommand.InstantSkillShotParallel(key,  ECB, team, transform, aimInput, owner);
            ECB.SetComponent<CombineCharsComponent>(key, ability, new() { MagicalPower = magicalPower.Value });

            aoeAbilityCommand.CancelParallel(ECB, key, owner, activatedAbilitiesCommands);
            
            cooldownTargetTicks.UpdateCooldown(
                abilityCooldownTicks, NetTime, aoeAbilityCommand.AbilityIndex, IsServer);
        }
    }
}