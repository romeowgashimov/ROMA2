using Logic.Common;
using Unity.Entities;

namespace Logic.Client
{
    public partial class AbilityInputSystem :  SystemBase
    {
        private MobaInputActions _inputActions;

        protected override void OnCreate()
        {
            RequireForUpdate<GameplayingTag>();
            _inputActions = new();
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
            AbilityInput newAbilityInput = new()
            {
                NeedToConfirmAbilities = true
            };
            
            if(_inputActions.GameplayMap.Ability1.WasPressedThisFrame())
                newAbilityInput.Ability1.Set();
            
            if(_inputActions.GameplayMap.Ability2.WasPressedThisFrame())
                newAbilityInput.Ability2.Set();
            
            if(_inputActions.GameplayMap.ConfirmAbility.WasPressedThisFrame())
                newAbilityInput.ConfirmAbility.Set();
            
            if(_inputActions.GameplayMap.CancelAbility.WasPressedThisFrame())
                newAbilityInput.CancelAbility.Set();

            foreach(RefRW<AbilityInput> abilityInput in SystemAPI.Query<RefRW<AbilityInput>>())
                abilityInput.ValueRW = newAbilityInput;
        }
    }
}