using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class AoeAbilityCommandAuthoring : MonoBehaviour
    {
        public GameObject AoeAbilityPrefab;
        
        private class SkillShotAbilityBaker : Baker<AoeAbilityCommandAuthoring>
        {
            public override void Bake(AoeAbilityCommandAuthoring commandAuthoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<AbilityCommand>(entity);
                AddComponent(entity, new AoeAbilityCommand
                {
                    Prefab = GetEntity(commandAuthoring.AoeAbilityPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}