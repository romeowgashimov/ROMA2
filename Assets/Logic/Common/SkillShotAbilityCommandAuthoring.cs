using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class SkillShotAbilityCommandAuthoring : MonoBehaviour
    {
        public GameObject SkillShotAbilityPrefab;
        
        private class SkillShotAbilityBaker : Baker<SkillShotAbilityCommandAuthoring>
        {
            public override void Bake(SkillShotAbilityCommandAuthoring commandAuthoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<AbilityCommand>(entity);
                AddComponent(entity, new SkillShotAbilityCommand
                {
                    Prefab = GetEntity(commandAuthoring.SkillShotAbilityPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}