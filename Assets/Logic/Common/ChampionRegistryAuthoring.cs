using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

#if UNITY_EDITOR
using static UnityEditor.AssetDatabase;
#endif

namespace Logic.Common
{
    public class ChampionRegistryAuthoring : MonoBehaviour
    {
        public List<GameObject> ChampionPrefabs;
        // В будущем нужно сделать config всех префабов через ScriptableObject
        [HideInInspector] public List<EntityPrefabReference> ChampionPrefabReferences;
        
        public bool Refresh;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Refresh &&
                (ChampionPrefabs == null || ChampionPrefabReferences.Count == ChampionPrefabs.Count)) return;
            
            Refresh = false;
                
            ChampionPrefabReferences = (from hero in ChampionPrefabs
                    where hero != null 
                    let path = GetAssetPath(hero)
                    where !string.IsNullOrEmpty(path)
                    let guidStr = AssetPathToGUID(path)
                    where !string.IsNullOrEmpty(guidStr)
                    select new EntityPrefabReference(new Hash128(guidStr)))
                .ToList();

            // Помечаем объект грязным для сохранения
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        
        private class HeroRegistryBaker : Baker<ChampionRegistryAuthoring>
        {
            public override void Bake(ChampionRegistryAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                DynamicBuffer<ChampionPrefabElement> heroes = AddBuffer<ChampionPrefabElement>(entity);
                foreach (EntityPrefabReference hero in authoring.ChampionPrefabReferences) 
                    heroes.Add(new() { Value = hero });
            } 
        }
    }
}