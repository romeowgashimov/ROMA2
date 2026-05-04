using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.quaternion;
using static Unity.Transforms.LocalTransform;

namespace Logic.Common.ACs
{
    public static class AbilityCommandExtensions
    {
        public static void Send<T>(
            this T anyCommand,
            AbilityCommand mainCommand,
            Entity commandEntity,
            EntityManager mgr,
            EntityCommandBuffer ecb) 
            where T : unmanaged, IAbilityCommand
        {
            Entity owner = mainCommand.Owner;
            if (mgr.HasComponent<T>(owner)) ecb.SetComponentEnabled<T>(owner, true);
            else
            {
                ecb.AddComponent(owner, new T
                {
                    Prefab = anyCommand.Prefab,
                    AbilityIndex = mainCommand.AbilityIndex,
                    // Менять на глобальный конфиг настроек
                    NeedToConfirmAbilities = mainCommand.NeedToConfirmAbilities 
                });
            }
            ecb.DestroyEntity(commandEntity);
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
    
        public static Entity InstantSkillShotParallel<T>(
            this T anyCommand,
            int key, 
            EntityCommandBuffer.ParallelWriter ecb,
            Team team, 
            LocalTransform transform, 
            AimInput aimInput, 
            Entity owner) 
            where T : unmanaged, IAbilityCommand
        {
            Entity ability = ecb.Instantiate(key, anyCommand.Prefab);
            LocalTransform newPosition = FromPositionRotation(transform.Position,
                LookRotationSafe(aimInput.Value, math.up()));
            ecb.SetComponent(key, ability, newPosition);
            ecb.SetComponent(key, ability, team);
            ecb.SetComponent(key, ability, new Owner { Value = owner });

            return ability;
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

        public static void DrawUI(
            this AbilityCommand mainCommand,
            EntityManager mgr,
            EntityCommandBuffer ecb,
            bool isServer)
        {
            if (isServer) return;
         
            Entity owner = mainCommand.Owner;
            bool needConfirm = mainCommand.NeedToConfirmAbilities;
            bool hasDraw = mgr.HasComponent<DrawAbilityUITag>(owner);
            bool hasUpdate = mgr.HasComponent<UpdateAbilityUITag>(owner);
        
            if (hasDraw) ecb.SetComponentEnabled<DrawAbilityUITag>(owner, needConfirm);
            else if (needConfirm) ecb.AddComponent<DrawAbilityUITag>(owner);

            if (!needConfirm) return;
            if (!hasUpdate) ecb.AddComponent<UpdateAbilityUITag>(owner);
            ecb.SetComponentEnabled<UpdateAbilityUITag>(owner, false);
        }
    }
}