using System.Collections.Generic;
using Logic.Client;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Entities.World;

namespace Logic.Common
{
    public class ChoiceChampionUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _choiceChampionPanel;
        [SerializeField] private GameObject _championsList;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private GameObject _countdownPanel;
        
        [SerializeField] private ChampionDatabase _championDatabase;
        
        // Make config for champions
        private List<uint> _championIds;
        // Test 
        private void Awake()
        {
            _championIds = new() { 0, 1 };
        }

        private void OnEnable()
        {
            if (DefaultGameObjectInjectionWorld == null) return;
            
            ClientRequestGameEntrySystem entryRequestSystem =
                DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ClientRequestGameEntrySystem>();
            
            if (entryRequestSystem == null) return;
            
            ChampionChoiceIcon[] icons = _championsList.GetComponentsInChildren<ChampionChoiceIcon>();
            for (int i = 0; i < icons.Length; ++i)
            {
                int i1 = i;
                icons[i].OnChosen += Choice;
                
                void Choice() => entryRequestSystem.ChoiceChampion(_championIds[i1]);
            }

            entryRequestSystem.OnChosen += OnDisable;
            _confirmButton.onClick.AddListener(entryRequestSystem.ConfirmChampion);
        }

        private void OnDisable()
        {
            if (DefaultGameObjectInjectionWorld == null) return;
            
            _choiceChampionPanel.SetActive(false);
            ClientRequestGameEntrySystem entryRequestSystem =
                DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ClientRequestGameEntrySystem>();
            
            if(entryRequestSystem == null) return;
            
            entryRequestSystem.OnChosen -= OnDisable;
            
            ChampionChoiceIcon[] icons = _championsList.GetComponentsInChildren<ChampionChoiceIcon>();
            for (int i = 0; i < icons.Length; i++)
                icons[i].RemoveAllListeners();
            
            _confirmButton.onClick.RemoveAllListeners();
            
            //_countdownPanel.SetActive(true);
        }
    }
}