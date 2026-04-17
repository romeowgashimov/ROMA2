using Unity.Entities;
using Unity.NetCode;

namespace Logic.Common
{
    public static class CooldownTicksExtensions
    {
        public static bool IsOnCooldown(this DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            NetworkTime networkTime, int abilityIndex)
        {
            NetworkTick currentTick = networkTime.ServerTick;
            bool isOnCooldown = true;

            for (uint i = 0u; i < networkTime.SimulationStepBatchSize; i++)
            {
                NetworkTick testTick = currentTick;
                testTick.Subtract(i);

                if (!cooldownTargetTicks.GetDataAtTick(testTick, out AbilityCooldownTargetTicks curTargetTicks))
                    curTargetTicks[abilityIndex] = NetworkTick.Invalid;

                if (curTargetTicks[abilityIndex] != NetworkTick.Invalid &&
                    curTargetTicks[abilityIndex].IsNewerThan(currentTick)) continue;
                
                isOnCooldown = false;
                break;
            }

            return isOnCooldown;
        }

        public static void UpdateCooldown(this DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            AbilityCooldownTicks cooldownTicks, NetworkTime networkTime, int abilityIndex)
        {
            NetworkTick currentTick = networkTime.ServerTick;
            
            cooldownTargetTicks.GetDataAtTick(currentTick, out AbilityCooldownTargetTicks curTargetTicks);
            
            NetworkTick newCooldownTargetTicks = currentTick;
            newCooldownTargetTicks.Add(cooldownTicks[abilityIndex]);
            curTargetTicks[abilityIndex] = newCooldownTargetTicks;

            NetworkTick nextTick = currentTick;
            nextTick.Add(1u);
            curTargetTicks.Tick = nextTick;

            cooldownTargetTicks.AddCommandData(curTargetTicks);
        }
    }
}