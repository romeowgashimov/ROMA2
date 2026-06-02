using ROMA2.Logic.Client.Controllers;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Client.Models
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
                    curTargetTicks.Ability1 = NetworkTick.Invalid;
                    curTargetTicks.Ability2 = NetworkTick.Invalid;
                }

                for (int i = 0; i < curTargetTicks.GetAbilityCount(); i++)
                {
                    float fillAmount;
                    NetworkTick abilityTick = curTargetTicks.GetAbilityByTick(i);
                    if (abilityTick == NetworkTick.Invalid ||
                        currentTick.IsNewerThan(abilityTick))
                        fillAmount = 0;
                    else
                    {
                        uint remainTickCount = abilityTick.TickIndexForValidTick -
                                               currentTick.TickIndexForValidTick;
                        fillAmount = (float)remainTickCount / cooldownTicks[i];
                    }
                    abilityCooldownUIController.UpdateMask(i, fillAmount);
                }
            }
        }
    }
}