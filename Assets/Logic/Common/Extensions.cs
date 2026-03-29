using Unity.Entities;
using Unity.NetCode;

namespace Logic.Common
{
    public static class Extensions
    {
        public static bool IsOnCooldown(this DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            NetworkTime networkTime)
        {
            NetworkTick currentTick = networkTime.ServerTick;
            bool isOnCooldown = true;

            for (uint i = 0u; i < networkTime.SimulationStepBatchSize; i++)
            {
                NetworkTick testTick = currentTick;
                testTick.Subtract(i);

                if (!cooldownTargetTicks.GetDataAtTick(testTick, out AbilityCooldownTargetTicks curTargetTicks))
                    curTargetTicks.SkillShotAbility = NetworkTick.Invalid;

                if (curTargetTicks.SkillShotAbility != NetworkTick.Invalid &&
                    curTargetTicks.SkillShotAbility.IsNewerThan(currentTick)) continue;
                
                isOnCooldown = false;
                break;
            }

            return isOnCooldown;
        }
    }
}