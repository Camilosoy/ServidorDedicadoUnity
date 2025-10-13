using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    [Tooltip("Base URL sin /{gameId}/{playerId}. Ej: http://127.0.0.1:5005/server")]
    public string baseUrl = "http://127.0.0.1:5005/server";

    public event Action<int , ServerData> OnDataReceived;

    public IEnumerator GetPlayerData(string gameId, string playerId)
    {
        string url = $"{baseUrl}/{gameId}/{playerId}";
        using (UnityWebRequest wr = UnityWebRequest.Get(url))
        {
            yield return wr.SendWebRequest();
            if (wr.result == UnityWebRequest.Result.ConnectionError || wr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"GET Error: Cannot connect to destination host (code {wr.responseCode}) url={url}");
            }
            else
            {
                var json = wr.downloadHandler.text;
                var data = JsonUtility.FromJson<ServerData>(string.IsNullOrEmpty(json) ? "{}" : json);
                int id = 0; int.TryParse(playerId, out id);
                // Log para diagnóstico
                Debug.Log($"[ApiClient] GET OK id={id} -> ({data.posX:F2},{data.posY:F2},{data.posZ:F2}) paused={data.paused}");
                OnDataReceived?.Invoke(id, data);
            }
        }
    }

    public IEnumerator PostPlayerData(string gameId, string playerId, ServerData data)
    {
        string url = $"{baseUrl}/{gameId}/{playerId}";
        string jsonData = JsonUtility.ToJson(data);

        using (UnityWebRequest wr = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            wr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            wr.downloadHandler = new DownloadHandlerBuffer();
            wr.SetRequestHeader("Content-Type", "application/json");

            yield return wr.SendWebRequest();

            if (wr.result == UnityWebRequest.Result.ConnectionError || wr.result == UnityWebRequest.Result.ProtocolError)
                Debug.LogError($"POST Error: Cannot connect to destination host (code {wr.responseCode}) url={url}");
            else
                Debug.Log($"[ApiClient] POST OK id={playerId} body={jsonData}");
        }
    }
}
