using Unity.Entities;
using Unity.NetCode;

namespace Logic.Common
{
    public static class CooldownTicksExtensions
    {
        public static bool IsOnCooldown(this DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            NetworkTime networkTime, AbilityType abilityType)
        {
            NetworkTick currentTick = networkTime.ServerTick;
            bool isOnCooldown = true;

            for (uint i = 0u; i < networkTime.SimulationStepBatchSize; i++)
            {
                NetworkTick testTick = currentTick;
                testTick.Subtract(i);

                if (!cooldownTargetTicks.GetDataAtTick(testTick, out AbilityCooldownTargetTicks curTargetTicks))
                    curTargetTicks[abilityType] = NetworkTick.Invalid;

                if (curTargetTicks[abilityType] != NetworkTick.Invalid &&
                    curTargetTicks[abilityType].IsNewerThan(currentTick)) continue;
                
                isOnCooldown = false;
                break;
            }

            return isOnCooldown;
        }

        public static void UpdateCooldown(this DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            AbilityCooldownTicks cooldownTicks, NetworkTime networkTime, AbilityType abilityType)
        {
            NetworkTick currentTick = networkTime.ServerTick;
            
            cooldownTargetTicks.GetDataAtTick(currentTick, out AbilityCooldownTargetTicks curTargetTicks);
            
            NetworkTick newCooldownTargetTicks = currentTick;
            newCooldownTargetTicks.Add(cooldownTicks[abilityType]);
            curTargetTicks[abilityType] = newCooldownTargetTicks;

            NetworkTick nextTick = currentTick;
            nextTick.Add(1u);
            curTargetTicks.Tick = nextTick;

            cooldownTargetTicks.AddCommandData(curTargetTicks);
        }
    }
}