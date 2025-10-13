using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ApiClient api;
    [SerializeField] private List<PlayerController> players;

    [Header("Config")]
    public string gameId = "room-1";

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (api == null) api = FindObjectOfType<ApiClient>();
    }

    private void Start()
    {
        if (api != null) api.OnDataReceived += OnDataReceived;
    }

    private void OnDestroy()
    {
        if (api != null) api.OnDataReceived -= OnDataReceived;
    }

    // ---- UTIL: buscar por playerId (NO por índice) ----
    private PlayerController FindById(int id)
    {
        if (players == null) return null;
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p != null && p.GetPlayerId() == id) return p;
        }
        return null;
    }

    // Pull remoto
    public void GetPlayerData(int playerId)
    {
        StartCoroutine(api.GetPlayerData(gameId, playerId.ToString()));
    }

    // Push local
    public void SendPlayerPosition(int playerId)
    {
        var p = FindById(playerId);
        if (p == null) return;

        Vector3 pos = p.GetPosition();
        ServerData data = new ServerData { posX = pos.x, posY = pos.y, posZ = pos.z, paused = IsPaused };
        Debug.Log($"[GM] Send id={playerId} pos={pos}");
        StartCoroutine(api.PostPlayerData(gameId, playerId.ToString(), data));
    }

    // Callback desde ApiClient
    public void OnDataReceived(int playerId, ServerData data)
    {
        var p = FindById(playerId);
        if (p == null) return;

        Debug.Log($"[GM] Recv id={playerId} => ({data.posX:F2},{data.posY:F2},{data.posZ:F2}) paused={data.paused}");
        ApplyPause(data.paused);
        p.MovePlayer(new Vector3(data.posX, data.posY, data.posZ));
    }

    public void ApplyPause(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;
        Time.timeScale = IsPaused ? 0f : 1f;
    }

    public List<PlayerController> GetPlayers() => players;

    // Activa/quita el control local por ID (se llama desde la UI)
    public void RebindLocalControl(int localId)
    {
        if (players == null) return;
        foreach (var p in players)
        {
            if (!p) continue;
            bool shouldBeLocal = (p.GetPlayerId() == localId);
            p.isLocal = shouldBeLocal;

            var mover = p.GetComponent<LocalInputMover>();
            if (shouldBeLocal)
            {
                if (!mover) p.gameObject.AddComponent<LocalInputMover>();
            }
            else
            {
                if (mover) Destroy(mover);
            }
        }
        Debug.Log($"[GM] Local control bound to id={localId}");
    }
}
