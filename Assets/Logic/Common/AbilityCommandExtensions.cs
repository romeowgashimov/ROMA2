using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    public static class AbilityCommandExtensions
    {
        public static void Send<T>(
            this T anyCommand,
            AbilityCommand mainCommand,
            Entity commandEntity,
            EntityManager mgr,
            EntityCommandBuffer ecb,
            bool isServer) 
            where T : unmanaged, IAbilityCommand
        {
            Entity owner = mainCommand.Owner;
            if (mgr.HasComponent<T>(owner)) ecb.SetComponentEnabled<T>(owner, true);
            else
            {
                T newCommand = new()
                {
                    Prefab = anyCommand.Prefab,
                    AbilityIndex = mainCommand.AbilityIndex,
                    // Менять на глобальный конфиг настроек
                    NeedToConfirmAbilities = mainCommand.NeedToConfirmAbilities
                };
                ecb.AddComponent(owner, newCommand);
            }
            ecb.SetComponentEnabled<AimingTag>(owner, true);
            ecb.DestroyEntity(commandEntity);

            if (isServer) return;

            bool needConfirm = mainCommand.NeedToConfirmAbilities;
            bool hasDraw = mgr.HasComponent<DrawAbilityUITag>(owner);
            bool hasUpdate = mgr.HasComponent<UpdateAbilityUITag>(owner);
        
            if (hasDraw) ecb.SetComponentEnabled<DrawAbilityUITag>(owner, needConfirm);
            else if (needConfirm) ecb.AddComponent<DrawAbilityUITag>(owner);

            if (!needConfirm) return;
            if (!hasUpdate) ecb.AddComponent<UpdateAbilityUITag>(owner);
            ecb.SetComponentEnabled<UpdateAbilityUITag>(owner, false);
        }

        public static void CancelParallel<T>(
            this T anyCommand, 
            EntityCommandBuffer.ParallelWriter ecb,
            int key,
            Entity owner,
            RefRW<ActivatedAbilitiesCommands> activatedAbilitiesCommands)
            where T : unmanaged, IAbilityCommand
        {
            ecb.SetComponentEnabled<T>(key, owner, false);
            activatedAbilitiesCommands.ValueRW[anyCommand.AbilityIndex] = false;
        }
    
        public static void InstantSkillShotParallel<T>(
            this T anyCommand,
            int key, 
            bool isServer,
            NetworkTime netTime,
            EntityCommandBuffer.ParallelWriter ecb,
            AbilityInput abilityInput,
            AbilityCooldownTicks abilityCooldownTicks, 
            Team team, 
            RefRW<ActivatedAbilitiesCommands> activatedAbilitiesCommands,
            LocalTransform transform, 
            DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            AimInput aimInput, 
            Entity owner) 
            where T : unmanaged, IAbilityCommand
        {
            if (!anyCommand.IsConfirmParallel(key, ecb, activatedAbilitiesCommands, abilityInput, owner)) return;
            
            Entity skillShot = ecb.Instantiate(key, anyCommand.Prefab);
            LocalTransform newPosition = LocalTransform.FromPositionRotation(transform.Position,
                quaternion.LookRotationSafe(aimInput.Value, math.up()));
            ecb.SetComponent(key, skillShot, newPosition);
            ecb.SetComponent(key, skillShot, team);
            // ECB.SetComponent(key, skillShot, new OwnerTag { Value = owner });
            
            anyCommand.CancelParallel(ecb, key, owner, activatedAbilitiesCommands);

            if (isServer) return;

            cooldownTargetTicks.UpdateCooldown(abilityCooldownTicks, netTime, anyCommand.AbilityIndex);
        }

        public static bool IsConfirmParallel<T>(
            this T anyCommand,
            int key,
            EntityCommandBuffer.ParallelWriter ecb,
            RefRW<ActivatedAbilitiesCommands> activatedAbilitiesCommands,
            AbilityInput abilityInput,
            Entity owner)
            where T : unmanaged, IAbilityCommand
        {
            if (!anyCommand.NeedToConfirmAbilities) return true;
            if (abilityInput.ConfirmAbility.IsSet) return true;
            if (abilityInput.CancelAbility.IsSet) 
                anyCommand.CancelParallel(ecb, key, owner, activatedAbilitiesCommands);
            return false;
        }
    }
}