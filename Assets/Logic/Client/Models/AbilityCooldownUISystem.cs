using Logic.Common;
using Unity.Entities;
using Unity.NetCode;

namespace Logic.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct AbilityCooldownUISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            //Здесь ошибка, мне кажется
            AbilityCooldownUIController abilityCooldownUIController = AbilityCooldownUIController.Instance;

            foreach ((DynamicBuffer<AbilityCooldownTargetTicks> targetTicks,
                         AbilityCooldownTicks cooldownTicks) in SystemAPI
                         .Query<DynamicBuffer<AbilityCooldownTargetTicks>, AbilityCooldownTicks>())
            {
                if (!targetTicks.GetDataAtTick(currentTick, out AbilityCooldownTargetTicks curTargetTicks))
                {
                    curTargetTicks.AoeAbility = NetworkTick.Invalid;
                    curTargetTicks.SkillShotAbility = NetworkTick.Invalid;
                }

                for (int i = 0; i < curTargetTicks.Length; i++)
                {
                    float fillAmount;
                    if (curTargetTicks[i] == NetworkTick.Invalid ||
                        currentTick.IsNewerThan(curTargetTicks[i]))
                        fillAmount = 0;
                    else
                    {
                        uint remainTickCount = curTargetTicks[i].TickIndexForValidTick -
                                                  currentTick.TickIndexForValidTick;
                        fillAmount = (float)remainTickCount / cooldownTicks[i];
                    }
                    abilityCooldownUIController.UpdateMask(i, fillAmount);
                }
            }
        }
    }
}