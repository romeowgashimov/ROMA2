using ROMA2.Logic.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

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
            
            state.Dependency = new DetectTargetEntityJob
            {
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
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

    [WithNone(typeof(MinionTag))]
    public partial struct DetectTargetEntityJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        
        private void Execute(
            ref TargetEntity targetEntity,
            in DetectionRadius detectionRadius,
            in LocalTransform ownerTransform)
        {
            Entity target = targetEntity.Value;
            if (target == Entity.Null) return;
            
            if (TransformLookup.TryGetComponent(target, out LocalTransform targetTransform) 
                && math.distancesq(ownerTransform.Position, targetTransform.Position) > detectionRadius.Value * detectionRadius.Value)
                targetEntity.Value = Entity.Null;
        }
    }
}