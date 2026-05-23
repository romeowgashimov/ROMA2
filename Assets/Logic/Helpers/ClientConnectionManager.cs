using Assets.Logic.Client;
using Logic.Common;
using ROMA2.Logic.Server.Initialization;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROMA2.Logic.Helpers
{
    public class ClientConnectionManager : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _addressField;
        [SerializeField] private TMP_InputField _portField;
        [SerializeField] private TMP_Dropdown _connectionModeDropdown;
        [SerializeField] private TMP_Dropdown _teamDropdown;
        [SerializeField] private Button _startButton;
        [SerializeField] private TMP_InputField _minPlayersToStartField;

        private ushort Port => ushort.Parse(_portField.text);
        private string Address => _addressField.text;
        private int MinPlayersToStart
        {
            get
            {
                int playersCount = int.Parse(_minPlayersToStartField.text);
                return math.clamp(playersCount, 1, 10);
            }
        }

        private void OnEnable()
        {
            _connectionModeDropdown.onValueChanged.AddListener(OnConnectionModeChanged);
            _startButton.onClick.AddListener(OnButtonConnect);
            OnConnectionModeChanged(_connectionModeDropdown.value);
        }

        private void OnDisable()
        {
            _connectionModeDropdown.onValueChanged.RemoveAllListeners();
            _startButton.onClick.RemoveAllListeners();
        }

        private void OnConnectionModeChanged(int connectionMode)
        {
            string buttonLabel;
            _startButton.enabled = true;

            switch (connectionMode)
            {
                case 0:
                    buttonLabel = "Start Host";
                    break;
                case 1:
                    buttonLabel = "Start Server";
                    break;
                case 2:
                    buttonLabel = "Start Client";
                    break;
                default:
                    buttonLabel = "ERROR";
                    _startButton.enabled = false;
                    break;
            }

            TextMeshProUGUI buttonText = _startButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = buttonLabel;
        }

        private void OnButtonConnect()
        {
            DestroyLocalSimulationWorld();
            SceneManager.LoadScene(1);

            switch (_connectionModeDropdown.value)
            {
                case 0:
                    StartServer();
                    StartClient();
                    break;
                case 1:
                    StartServer();
                    break;
                case 2:
                    StartClient();
                    break;
                default:
                    Debug.LogError("Error: Unknown connection mode", gameObject);
                    break;
            }
        }

        private static void DestroyLocalSimulationWorld()
        {
            foreach (World world in World.All)
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
        }

        private void StartServer()
        {
            GameStartSettings.MinPlayersToStart = MinPlayersToStart;
            World serverWorld = ClientServerBootstrap.CreateServerWorld("Server");

            NetworkEndpoint serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(Port);
            {
                using EntityQuery networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
            }
        }

        private void StartClient()
        {
            World clientWorld = ClientServerBootstrap.CreateClientWorld("Client");

            NetworkEndpoint connectionEndpoint = NetworkEndpoint.Parse(Address, Port);
            {
                using EntityQuery networkDriverQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, connectionEndpoint);
            }

            World.DefaultGameObjectInjectionWorld = clientWorld;

            TeamType team = _teamDropdown.value switch
            {
                0 => TeamType.AutoAssign,
                1 => TeamType.Blue,
                2 => TeamType.Red,
                _ => TeamType.None
            };

            Entity teamRequestEntity = clientWorld.EntityManager.CreateEntity();
            clientWorld.EntityManager.AddComponentData(teamRequestEntity, new ClientTeamRequest { Value = team });
        }
    }
}