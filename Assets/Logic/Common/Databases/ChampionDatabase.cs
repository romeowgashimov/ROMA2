using System;
using System.Collections.Generic;
using UnityEngine;

namespace ROMA2.Logic.Common.Databases
{
    [Serializable]
    public struct ChampionConfigEntry
    {
        public int Id;
        public GameObject Prefab;
        public int MoveSpeed;
        public int HealthPoints;
        public int AttackSpeed;
        public float AttackRadius;
        public GameObject AttackPrefab;
    }

    [CreateAssetMenu(fileName = "ChampionDatabase", menuName = "ROMA2/Champion Database")]
    public class ChampionDatabase : ScriptableObject
    {
        public List<ChampionConfigEntry> Configs;
        
        public ChampionConfigEntry FindConfig(int id) => 
            Configs.Find(config => config.Id == id);
    }
}