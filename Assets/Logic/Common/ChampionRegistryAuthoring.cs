using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

#if UNITY_EDITOR
using UnityEditor;
using static UnityEditor.AssetDatabase;
#endif

namespace Logic.Common
{
    public class ChampionRegistryAuthoring : MonoBehaviour
    {
        public ChampionDatabase Database;
        
        [HideInInspector] 
        public List<ChampionPrefabElement> ChampionPrefabReferences;
        
        public bool Refresh;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Refresh) return;
            Refresh = false;
            SyncWithDatabase();
        }
        
        public void SyncWithDatabase()
        {
            if (Database == null || Database.Configs == null) return;

            ChampionPrefabReferences = (from hero in Database.Configs
                    where hero.Prefab != null 
                    let path = GetAssetPath(hero.Prefab)
                    where !string.IsNullOrEmpty(path)
                    let guidStr = AssetPathToGUID(path)
                    where !string.IsNullOrEmpty(guidStr)
                    select new ChampionPrefabElement 
                        { 
                            Id = hero.Id,
                            Value = new(new Hash128(guidStr))
                        })
                .ToList();

            EditorUtility.SetDirty(this);
        }
#endif

        private class HeroRegistryBaker : Baker<ChampionRegistryAuthoring>
        {
            public override void Bake(ChampionRegistryAuthoring authoring)
            {
                if (authoring.Database == null) return;
                
#if UNITY_EDITOR
                if (authoring.ChampionPrefabReferences == null)
                    authoring.SyncWithDatabase();
#endif
                
                Entity entity = GetEntity(TransformUsageFlags.None);
                DynamicBuffer<ChampionPrefabElement> heroes = AddBuffer<ChampionPrefabElement>(entity);
                
                foreach (ChampionPrefabElement reference in authoring.ChampionPrefabReferences) 
                    heroes.Add(reference);
            }
        }
    }
}