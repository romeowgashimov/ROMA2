using Logic.Common;
using Unity.Entities;

namespace Logic.Client
{
    public partial class AbilityInputSystem :  SystemBase
    {
        private MobaInputActions _inputActions;

        protected override void OnCreate()
        {
            _inputActions = new MobaInputActions();
        }

        protected override void OnStartRunning()
        {
            _inputActions.Enable();
        }

        protected override void OnStopRunning()
        {
            _inputActions.Disable();
        }

        protected override void OnUpdate()
        {
            AbilityInput newAbilityInput = new();
            
            if(_inputActions.GameplayMap.AoeAblility.WasPressedThisFrame())
                newAbilityInput.AoeAbility.Set();
            
            if(_inputActions.GameplayMap.SkillShotAbility.WasPressedThisFrame())
                newAbilityInput.SkillShotAbility.Set();
            
            if(_inputActions.GameplayMap.ConfirmSkillShotAbility.WasPressedThisFrame())
                newAbilityInput.ConfirmSkillShotAbility.Set();
            
            foreach(RefRW<AbilityInput> abilityInput in SystemAPI.Query<RefRW<AbilityInput>>())
                abilityInput.ValueRW = newAbilityInput;
        }
    }
}