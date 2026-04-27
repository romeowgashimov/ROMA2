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

            // Вынести в отдельный метод отрисовки
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
            LocalTransform newPosition = LocalTransform.FromPositionRotation(transform.Position,
                quaternion.LookRotationSafe(aimInput.Value, math.up()));
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
    }
}