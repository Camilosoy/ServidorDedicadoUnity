using UnityEngine;
using UnityEngine.UI;

public class HostControls : MonoBehaviour
{
    [Header("Refs")]
    public ApiServer server;
    public GameManager gameManager;

    [Header("UI")]
    public Text statsText;
    public InputField kickIdField;

    [Header("Config")]
    public string gameId = "room-1";

    private float refreshTimer = 0f;

    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= 0.5f)
        {
            refreshTimer = 0f;
            if (statsText && gameManager)
            {
                int count = gameManager.GetPlayers() != null ? gameManager.GetPlayers().Count : 0;
                statsText.text = $"Host PORT: {server?.port}\nGameId: {gameId}\nPaused: {gameManager.IsPaused}\nPlayers (scene): {count}";
            }
        }
    }

    public void TogglePause()
    {
        if (!server || !gameManager) return;
        bool paused = server.TogglePause(gameId);
        gameManager.ApplyPause(paused);
    }

    public void Kick()
    {
        if (!server) return;
        if (!int.TryParse(kickIdField.text, out int id)) return;
        bool ok = server.Kick(gameId, id);
        Debug.Log($"Kick {id}: {(ok ? "OK" : "No encontrado")}");
    }
}
