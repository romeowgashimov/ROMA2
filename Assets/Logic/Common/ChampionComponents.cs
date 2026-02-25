using Assets.Logic.Common;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    public struct ChampTag : IComponentData { }
    
    public struct NewChampTag : IComponentData { }
    
    public struct OwnerChampTag : IComponentData { }
    
    public struct Team : IComponentData
    {
        [GhostField] public TeamType Value;
    }
    
    public struct MoveSpeed : IComponentData
    {
        public float Value;
    }
    
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct MoveTargetPosition : IInputComponentData
    {
        [GhostField(Quantization = 0)] public float3 Value;
        public bool Flag;
    }
    
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct AbilityInput : IInputComponentData
    {
        [GhostField] public InputEvent AoeAbility;
        [GhostField] public InputEvent SkillShotAbility;
        [GhostField] public InputEvent ConfirmSkillShotAbility;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct AimInput : IInputComponentData
    {
        [GhostField(Quantization = 0)] public float3 Value;
    }
}