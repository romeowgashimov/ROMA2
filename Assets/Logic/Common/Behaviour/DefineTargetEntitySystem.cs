using Logic.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Behaviour
{ 
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct DefineTargetEntitySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new DefineTargetEntityJob
            {
                HealthLookup = SystemAPI.GetComponentLookup<CurrentHealthPoints>(true),
                TeamLookup = SystemAPI.GetComponentLookup<Team>(true),
            }.ScheduleParallel(state.Dependency);
        }
    }
    
    public partial struct DefineTargetEntityJob : IJobEntity
    {
        // Можем выбрать в качестве цели объект, имеющий здоровье
        [ReadOnly] public ComponentLookup<CurrentHealthPoints> HealthLookup;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;
        
        private void Execute(
            ref TargetEntity targetEntity,
            in DetectionRadius detectionRadius,
            in SelectedEntity selectedEntity,
            in AbilityInput input,
            in Team team)
        {
            Entity selected = selectedEntity.Value;
            bool isBasicAttack = input.BasicAttack.IsSet;
            if (selected == Entity.Null && isBasicAttack)
            {
                targetEntity.Value = Entity.Null;
                return;
            }

            if (!isBasicAttack) return;
            if (HealthLookup.HasComponent(selected) 
                && TeamLookup.TryGetComponent(selected, out Team selectedTeam))
                if (selectedTeam.Value != team.Value)
                    targetEntity.Value = selected;
        }
    }
}