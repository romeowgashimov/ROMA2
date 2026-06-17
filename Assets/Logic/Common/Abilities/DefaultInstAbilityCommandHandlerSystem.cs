using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Common.Extensions;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;
using static Unity.Transforms.LocalTransform;

namespace ROMA2.Logic.Common.Abilities
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct DefaultInstAbilityCommandHandlerSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<DefaultInstAbilityCommand>();

            _query = SystemAPI.QueryBuilder()
                .WithAll<AbilityInput, AbilityCommands, AbilityCooldownTicks, Team,
                    LocalTransform, AbilityCooldownTargetTicks, AimInput>()
                .WithAll<DefaultInstAbilityCommand, ActivatedAbilitiesCommands,
                    AimingTag, Simulate, PhysicalPower, MagicalPower, CurrentMana>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick) return;

            EntityCommandBuffer ECB = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            bool isServer = state.WorldUnmanaged.IsServer();
            
            foreach ((DefaultInstAbilityCommand anyCommand, AbilityCommand mainCommand,
                         Entity commandEntity) in SystemAPI
                         .Query<DefaultInstAbilityCommand, AbilityCommand>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                // Если компонент уже есть на сущности, команда не приведёт к структурным изменениям
                ECB.AddComponent(mainCommand.Owner, new DefaultInstAbilityCommand
                {
                    Prefab = anyCommand.Prefab,
                    AbilityIndex = mainCommand.AbilityIndex,
                    // Менять на глобальный конфиг настроек
                    NeedToConfirmAbilities = mainCommand.NeedToConfirmAbilities ,
                    ManaCost = mainCommand.ManaCost
                });
                ECB.SetComponentEnabled<DefaultInstAbilityCommand>(mainCommand.Owner, true);
                ECB.DestroyEntity(commandEntity);
            }
            
            state.Dependency = new DefaultInstAbilityCommandJob
            {
                ECB = ECB.AsParallelWriter(),
                NetTime = netTime,
                IsServer = isServer
            }.ScheduleParallel(_query, state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct DefaultInstAbilityCommandJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public NetworkTime NetTime;
        public bool IsServer;

        private void Execute(
            [ChunkIndexInQuery] int key,
            in AbilityInput abilityInput,
            ref AbilityCooldownTicks abilityCooldownTicks,
            in Team team, 
            ref ActivatedAbilitiesCommands activatedAbilitiesCommands,
            in LocalTransform transform, 
            ref DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            in AimInput aimInput, 
            in DefaultInstAbilityCommand anyCommand, 
            in PhysicalPower physicalPower, 
            in MagicalPower magicalPower,
            ref CurrentMana currMana,
            Entity owner)
        {
            // Проверка, нужно ли подтверждать умение
            if (anyCommand.NeedToConfirmAbilities)
            {
                if (!abilityInput.ConfirmAbility.IsSet)
                {
                    if (abilityInput.CancelAbility.IsSet) 
                    {
                        ECB.SetComponentEnabled<DefaultInstAbilityCommand>(key, owner, false);
                        activatedAbilitiesCommands[anyCommand.AbilityIndex] = false;
                    }
                    return;
                }
            }

            // Инициализация умения
            Entity ability = ECB.Instantiate(key, anyCommand.Prefab);
            LocalTransform newPosition = FromPositionRotation(transform.Position,
                LookRotationSafe(aimInput.Value, up()));
            ECB.SetComponent(key, ability, newPosition);
            ECB.SetComponent(key, ability, team);
            ECB.SetComponent(key, ability, new Owner { Value = owner });
            ECB.SetComponent<CombineCharsComponent>(key, ability, new() 
            { 
                PhysicalPower = physicalPower.Value,
                MagicalPower = magicalPower.Value
            });
            ECB.SetComponent<AbilityIndex>(key, ability, new() { Value = anyCommand.AbilityIndex });
            
            // Выключение команды после инициализации
            ECB.SetComponentEnabled<DefaultInstAbilityCommand>(key, owner, false);
            activatedAbilitiesCommands[anyCommand.AbilityIndex] = false;

            // Обновление перезарядки
            cooldownTargetTicks.UpdateCooldown(
                abilityCooldownTicks, 
                NetTime, 
                anyCommand.AbilityIndex, 
                IsServer);
            
            // Уменьшение маны после применения
            currMana.Value -= anyCommand.ManaCost;
        }
    }
}