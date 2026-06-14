using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Abilities
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct DeathSphereAbilitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new DeathSphereAbilityJob().ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct DeathSphereAbilityJob : IJobEntity
    {
        public void Execute(
            in DeathSphereAbility ability,
            ref DynamicBuffer<TriggerEntityInfo> triggerInfoBuffer,
            in CombineCharsComponent combineCharsComponent,
            in DefaultDamage damage,
            ref DynamicBuffer<SendDamageElement> sendDamages,
            in AbilityIndex abilityIndex,
            in Owner owner)
        {
            if (triggerInfoBuffer.IsEmpty) return;

            for (int i = 0; i < triggerInfoBuffer.Length; ++i)
            {
                float magicalDamage = damage.MagicalDamage;
                magicalDamage += ability.MagicalPercentage / 100 * combineCharsComponent.MagicalPower;

                sendDamages.Add(new()
                {
                    MagicalDamage = magicalDamage,
                    Receiver = triggerInfoBuffer[i].Value,
                    Owner = owner.Value,
                    AbilityIndex = abilityIndex.Value
                });
                triggerInfoBuffer.RemoveAtSwapBack(i);
            }
        }
    }
}