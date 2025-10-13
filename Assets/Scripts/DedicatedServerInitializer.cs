using UnityEngine;

public class DedicatedServerInitializer : MonoBehaviour
{
    // Referencias a asignar en el Inspector
    public ApiServer apiServer;
    public GameManager gameManager;
    
    // Configuración por defecto para el servidor
    public int serverPort = 5005; 
    public string serverGameId = "room-1";

    private void Awake()
    {
        
        if (Application.isBatchMode)
        {
            StartServerOnly();
        }
        else
        {
            
            Destroy(gameObject); 
        }
    }

    private void StartServerOnly()
    {
        Debug.Log("[DedicatedServer] Iniciando Servidor de Autoridad...");

        
        if (apiServer != null)
        {
            apiServer.port = serverPort;
            apiServer.StartServer();
        }
        
        
        if (gameManager != null)
        {
            gameManager.gameId = serverGameId;
        }

        var ui = FindObjectOfType<MultiplayerUI>();
        if (ui != null) Destroy(ui.gameObject);
        
        var sync = FindObjectOfType<SyncLoop>();
        if (sync != null) Destroy(sync.gameObject);
        
        var hostControls = FindObjectOfType<HostControls>();
        if (hostControls != null) Destroy(hostControls.gameObject);

        
        foreach (var p in FindObjectsOfType<PlayerController>())
        {
            Destroy(p.gameObject);
        }

        Debug.Log("[DedicatedServer] Servidor Completo. Esperando clientes.");
    }
}