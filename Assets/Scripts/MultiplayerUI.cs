using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MultiplayerUI : MonoBehaviour
{
    [Header("Refs")]
    public ApiClient apiClient;
    public ApiServer apiServer;
    public GameManager gameManager;
    public SyncLoop sync;

    [Header("UI (Legacy)")]
    public InputField ipField;      
    public InputField portField;  
    public InputField gameIdField; 
    public InputField localIdField; 
    public GameObject disconnectedPanel;
    public GameObject connectedPanel;

    private bool running = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) EnsureDefaults(); 
    }
#endif

    private void Start()
    {
        EnsureDefaults();            
        RefreshPanels();
    }

    private void EnsureDefaults()
    {
        if (ipField && string.IsNullOrWhiteSpace(ipField.text)) ipField.text = "127.0.0.1";
        if (portField && string.IsNullOrWhiteSpace(portField.text)) portField.text = "5005";
        if (gameIdField && string.IsNullOrWhiteSpace(gameIdField.text)) gameIdField.text = "room-1";
        if (localIdField && string.IsNullOrWhiteSpace(localIdField.text)) localIdField.text = "0";
    }

    
    public void StartAsHost()
    {
        int port = ParseInt(portField ? portField.text : null, 5005);
        string gameId = string.IsNullOrWhiteSpace(gameIdField ? gameIdField.text : null) ? "room-1" : gameIdField.text.Trim();
        int localId = 0;

        if (apiServer != null)
        {
            apiServer.port = port;
            apiServer.StartServer();
        }
        else
        {
            Debug.LogWarning("[MultiplayerUI] ApiServer no asignado; este proceso no servirá como host.");
        }

        apiClient.baseUrl = $"http://127.0.0.1:{port}/server";
        gameManager.gameId = gameId;
        sync.localPlayerId = localId;

        gameManager.RebindLocalControl(localId);
        sync.StartSync();

        running = true;
        RefreshPanels();
        Debug.Log("[UI] Host started (LocalId=0)");
    }

    public void StartAsClient()
    {
        string ip = string.IsNullOrWhiteSpace(ipField.text) ? "127.0.0.1" : ipField.text.Trim();
        if (ip.Equals("localhost", System.StringComparison.OrdinalIgnoreCase)) ip = "127.0.0.1";

        int port = ParseInt(portField.text, 5005);
        string gameId = string.IsNullOrWhiteSpace(gameIdField.text) ? "room-1" : gameIdField.text.Trim();
        int localId = ParseInt(localIdField.text, 1);

        apiClient.baseUrl = $"http://{ip}:{port}/server";
        gameManager.gameId = gameId;
        sync.localPlayerId = localId;

        gameManager.RebindLocalControl(localId);

        Debug.Log($"[UI] Client started base={apiClient.baseUrl} gameId={gameId} localId={localId}");
        sync.StartSync();

        running = true;
        RefreshPanels();
    }

    public void StopAll()

    {
        sync.StopSync();
        

        apiServer?.StopServer(); 
        
        var playersToDestroy = gameManager.GetPlayers()?.ToList();
        if (playersToDestroy != null)
        {
            foreach (var p in playersToDestroy)
            {
                if (p != null) Destroy(p.gameObject);
            }
            gameManager.GetPlayers()?.Clear(); 
        }
        

        running = false;
        RefreshPanels();
        Debug.Log("[UI] Sesión detenida. Objetos de jugador limpiados.");
    }

    private int ParseInt(string s, int fallback) => int.TryParse(s, out var v) ? v : fallback;

    private void RefreshPanels()
    {
        if (connectedPanel) connectedPanel.SetActive(running);
        if (disconnectedPanel) disconnectedPanel.SetActive(!running);
    }
}
