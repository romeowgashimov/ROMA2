using ROMA2.Logic.Helpers.Bakers;
using Unity.Entities;
using UnityEngine;

namespace Logic.Common.Authorings
{
    public class AoeAbilityCommandAuthoring : MonoBehaviour
    {
        public GameObject AoeAbilityPrefab;
        
        private class SkillShotAbilityBaker : Baker<AoeAbilityCommandAuthoring>
        {
            public override void Bake(AoeAbilityCommandAuthoring commandAuthoring)
            {
                this.BakeAbilityCommand<AoeAbilityCommandAuthoring, 
                    AoeAbilityCommand>(commandAuthoring.AoeAbilityPrefab);
            }
        }
    }
}