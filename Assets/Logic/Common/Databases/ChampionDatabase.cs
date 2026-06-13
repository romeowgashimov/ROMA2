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
        public int MaxHealthPoints;
        public int AttackSpeed;
        public int MaxMana;
        public float HealthRegeneration;
        public float ManaRegeneration;
        public int PhysicalPower;
        public int MagicalPower;
        public int PhysicalArmor;
        public int MagicalArmor;
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