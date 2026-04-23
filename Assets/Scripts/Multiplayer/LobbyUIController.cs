using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using Cysharp.Threading.Tasks;
using Unity.Netcode; 

public class LobbyUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button createSessionButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinSessionButton;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private Button startGameButton;
    [Header("Nome da sua cena")]
    [SerializeField] private string gameSceneName = "GameScene";
    
    private NetworkManager networkManager;

    private void OnEnable()
    {
        createSessionButton.onClick.AddListener(OnCreateSessionClicked);
        joinSessionButton.onClick.AddListener(OnJoinSessionClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    private void OnDisable()
    {
        createSessionButton.onClick.RemoveListener(OnCreateSessionClicked);
        joinSessionButton.onClick.RemoveListener(OnJoinSessionClicked);
        startGameButton.onClick.RemoveListener(OnStartGameClicked);

        UnsubscribeNetworkEvents();
    }

    private void Start()
    {
        startGameButton.gameObject.SetActive(false);
        networkManager = NetworkManager.Singleton;
    }
    
    private void SetUIInteractable(bool isEnabled)
    {
        createSessionButton.interactable = isEnabled;
        joinSessionButton.interactable = isEnabled;
        joinCodeInput.interactable = isEnabled;
    }

    #region Buttons

        private async void OnCreateSessionClicked()
        {
            statusLabel.text = "Criando sessao...";
            SetUIInteractable(false);

            await SessionManager.Instance.CreateSessionAsHost();

            if (SessionManager.Instance.ActiveSession != null)
            {
                SubscribeNetworkEvents();
                
                RefreshLobbyUI(); 
            }
            else
            {
                statusLabel.text = "Falha ao criar sessao";
                SetUIInteractable(true);
            }
        }

        private async void OnJoinSessionClicked()
        {
            string code = joinCodeInput.text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                statusLabel.text = "Codigo invalido";
                return;
            }

            statusLabel.text = "Joining Session...";
            SetUIInteractable(false);

            await SessionManager.Instance.JoinSessionByCode(code);

            if (SessionManager.Instance.ActiveSession != null)
            {
                statusLabel.text = "Conectado em: " + SessionManager.Instance.ActiveSession.Code;
                
                SubscribeNetworkEvents();
            }
            else
            {
                statusLabel.text = "Falha ao conectar. Tente novamente.";
                SetUIInteractable(true);
            }
        }
        
        private void OnStartGameClicked()
        {
            bool isSessionOwner = networkManager.LocalClient != null && networkManager.LocalClient.IsSessionOwner;

            if (isSessionOwner && networkManager.ConnectedClientsIds.Count == 2)
            {
                startGameButton.interactable = false;
                networkManager.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

    #endregion
    
    #region Network Events
    
        private void SubscribeNetworkEvents()
        {
            if (networkManager == null) return;
            
            UnsubscribeNetworkEvents();
            networkManager.OnClientConnectedCallback += OnPlayerConnectionChanged;
            networkManager.OnClientDisconnectCallback += OnPlayerConnectionChanged;
        }

        private void UnsubscribeNetworkEvents()
        {
            if (networkManager == null) return;
            
            networkManager.OnClientConnectedCallback -= OnPlayerConnectionChanged;
            networkManager.OnClientDisconnectCallback -= OnPlayerConnectionChanged;
        }

        private void OnPlayerConnectionChanged(ulong clientId)
        {
            RefreshLobbyUI();
        }

        private void RefreshLobbyUI()
        {
            if (networkManager == null || !networkManager.IsListening) return;

            bool isSessionOwner = networkManager.LocalClient != null && networkManager.LocalClient.IsSessionOwner;

            if (isSessionOwner)
            {
                startGameButton.gameObject.SetActive(true);

                int currentPlayerCount = networkManager.ConnectedClientsIds.Count;

                startGameButton.interactable = (currentPlayerCount == 2);

                if (currentPlayerCount == 2)
                {
                    statusLabel.text = "Player 2 conectado";
                }
                else
                {
                    statusLabel.text = "Code: " + SessionManager.Instance.CurrentSessionCode;
                }
            }
            else
            {
                startGameButton.gameObject.SetActive(false);
            }
        }

    #endregion
}