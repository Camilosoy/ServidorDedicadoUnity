#if UNITY_EDITOR || UNITY_STANDALONE
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Servidor HTTP minimal con TcpListener (Unity 6 compatible).
/// Endpoints:
///   GET  /server/{gameId}/{playerId}
///   POST /server/{gameId}/{playerId}
///   GET  /server/{gameId}/pause
///   GET  /server/{gameId}/stats
///   POST /server/{gameId}/kick/{id}
/// </summary>
public class ApiServer : MonoBehaviour
{
    [Header("HTTP Host (TcpListener)")]
    public int port = 5005;

    private TcpListener listener;
    private Thread thread;
    private volatile bool running = false;

    // gameId -> room state
    private readonly Dictionary<string, RoomState> rooms = new Dictionary<string, RoomState>();
    private readonly object _lock = new object();

    [Serializable]
    private class PlayerState
    {
        public ServerData data = new ServerData();
        public DateTime lastSeen = DateTime.UtcNow;
        public bool kicked = false;
    }

    private class RoomState
    {
        public bool paused = false;
        public Dictionary<int, PlayerState> players = new Dictionary<int, PlayerState>();
    }

    public void StartServer()
    {
        if (running) return;
        try
        {
            listener = new TcpListener(IPAddress.IPv6Any, port);
            listener.Server.DualMode = true; // IPv4 + IPv6
            listener.Start();
            running = true;

            thread = new Thread(AcceptLoop) { IsBackground = true };
            thread.Start();

            Debug.Log($"[ApiServer] TcpListener escuchando en http://0.0.0.0:{port}/ (dual-stack)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ApiServer] Error al iniciar: {ex.Message}");
            StopServer();
        }
    }

    public void StopServer()
    {
        if (!running) return;
        running = false;
        try { listener?.Stop(); } catch {}
        listener = null;
        try { thread?.Join(150); } catch {}
        thread = null;
        Debug.Log("[ApiServer] Servidor detenido.");
    }

    private void OnDisable()         { if (running) StopServer(); }
    private void OnApplicationQuit() { StopServer(); }

    private void AcceptLoop()
    {
        while (true)
        {
            if (!running || listener == null) break;
            try
            {
                var client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
            catch (SocketException)
            {
                if (running) Debug.LogWarning("[ApiServer] SocketException en Accept.");
                break;
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception e)
            {
                if (running) Debug.LogError($"[ApiServer] Accept error: {e.Message}");
                if (!running) break;
            }
        }
    }

    private void HandleClient(TcpClient client)
    {
        using (client)
        using (var stream = client.GetStream())
        {
            stream.ReadTimeout = 5000;
            stream.WriteTimeout = 5000;
            try
            {
                ReadHttpRequest(stream, out string method, out string path, out _, out string body);
                if (string.IsNullOrEmpty(method))
                {
                    WriteHttp(stream, 400, "{\"error\":\"bad request\"}");
                    return;
                }

                string[] seg = path.Trim('/').Split('/');
                if (seg.Length < 3 || seg[0] != "server")
                {
                    WriteHttp(stream, 404, "{\"error\":\"invalid path\"}");
                    return;
                }
                string gameId = seg[1];
                string last = seg[2];

                if (int.TryParse(last, out int playerId))
                {
                    if (method == "GET")
                    {
                        Debug.Log($"[ApiServer] GET /{gameId}/{playerId}");
                        var sd = HandleGetPlayer(gameId, playerId);
                        WriteHttp(stream, 200, JsonUtility.ToJson(sd));
                        return;
                    }
                    else if (method == "POST")
                    {
                        Debug.Log($"[ApiServer] POST /{gameId}/{playerId} body={body}");
                        var incoming = JsonUtility.FromJson<ServerData>(string.IsNullOrEmpty(body) ? "{}" : body);
                        int code = HandlePostPlayer(gameId, playerId, incoming, out string resp);
                        WriteHttp(stream, code, resp);
                        return;
                    }
                    else
                    {
                        WriteHttp(stream, 405, "{\"error\":\"method not allowed\"}");
                        return;
                    }
                }

                if (last == "pause" && method == "GET")
                {
                    bool paused = TogglePause(gameId);
                    WriteHttp(stream, 200, $"{{\"paused\":{(paused ? "true" : "false")}}}");
                    return;
                }
                if (last == "stats" && method == "GET")
                {
                    WriteHttp(stream, 200, BuildStatsJson(gameId));
                    return;
                }
                if (last == "kick" && seg.Length >= 4 && int.TryParse(seg[3], out int toKick) && method == "POST")
                {
                    bool ok = Kick(gameId, toKick);
                    WriteHttp(stream, ok ? 200 : 404, $"{{\"kicked\":{(ok ? "true" : "false")}}}");
                    return;
                }

                WriteHttp(stream, 404, "{\"error\":\"unknown command\"}");
            }
            catch (Exception ex)
            {
                try { WriteHttp(stream, 500, $"{{\"error\":\"{ex.Message}\"}}"); } catch {}
            }
        }
    }

    private void ReadHttpRequest(NetworkStream stream, out string method, out string path, out Dictionary<string,string> headers, out string body)
    {
        method = ""; path = ""; body = "";
        headers = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        var buffer = new byte[8192];
        int bytesRead = 0;

        while (true)
        {
            int n = stream.Read(buffer, 0, buffer.Length);
            if (n <= 0) break;
            sb.Append(Encoding.UTF8.GetString(buffer, 0, n));
            string s = sb.ToString();

            int idx = s.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (idx >= 0)
            {
                string headerPart = s.Substring(0, idx);
                string[] lines = headerPart.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length > 0)
                {
                    string[] top = lines[0].Split(' ');
                    if (top.Length >= 2) { method = top[0].Trim(); path = top[1].Trim(); }
                }
                for (int i = 1; i < lines.Length; i++)
                {
                    int p = lines[i].IndexOf(':');
                    if (p > 0)
                    {
                        string k = lines[i].Substring(0, p).Trim();
                        string v = lines[i].Substring(p + 1).Trim();
                        headers[k] = v;
                    }
                }
                if (headers.TryGetValue("Content-Length", out string lenStr) && int.TryParse(lenStr, out int len))
                {
                    var bodyBytes = new byte[len];
                    int got = 0;
                    string after = s.Substring(idx + 4);
                    byte[] afterBytes = Encoding.UTF8.GetBytes(after);
                    int copy = Math.Min(afterBytes.Length, len);
                    Buffer.BlockCopy(afterBytes, 0, bodyBytes, 0, copy);
                    got += copy;
                    while (got < len)
                    {
                        int m = stream.Read(bodyBytes, got, len - got);
                        if (m <= 0) break;
                        got += m;
                    }
                    body = Encoding.UTF8.GetString(bodyBytes, 0, got);
                }
                break;
            }

            bytesRead += n;
            if (bytesRead > 2_000_000) break;
        }
    }

    private void WriteHttp(NetworkStream stream, int code, string json)
    {
        var status = code == 200 ? "OK" :
                     code == 404 ? "Not Found" :
                     code == 405 ? "Method Not Allowed" :
                     code == 403 ? "Forbidden" :
                     code == 400 ? "Bad Request" : "Internal Server Error";

        byte[] body = Encoding.UTF8.GetBytes(json ?? "");
        string header = $"HTTP/1.1 {code} {status}\r\n" +
                        "Content-Type: application/json\r\n" +
                        $"Content-Length: {body.Length}\r\n" +
                        "Connection: close\r\n\r\n";
        byte[] headBytes = Encoding.UTF8.GetBytes(header);
        stream.Write(headBytes, 0, headBytes.Length);
        stream.Write(body, 0, body.Length);
    }

    private ServerData HandleGetPlayer(string gameId, int playerId)
    {
        lock (_lock)
        {
            var room = GetRoom(gameId);
            if (!room.players.TryGetValue(playerId, out var ps))
            {
                ps = new PlayerState { data = new ServerData(), lastSeen = DateTime.UtcNow };
                room.players[playerId] = ps;
            }
            var sd = ps.data;
            sd.paused = room.paused;
            return sd;
        }
    }

    private int HandlePostPlayer(string gameId, int playerId, ServerData incoming, out string resp)
    {
        lock (_lock)
        {
            var room = GetRoom(gameId);
            if (!room.players.TryGetValue(playerId, out var ps))
            {
                ps = new PlayerState();
                room.players[playerId] = ps;
            }

            if (ps.kicked)
            {
                resp = "{\"error\":\"kicked\"}";
                return 403;
            }

            ps.data.posX = incoming.posX;
            ps.data.posY = incoming.posY;
            ps.data.posZ = incoming.posZ;
            ps.data.paused = room.paused;
            ps.lastSeen = DateTime.UtcNow;
        }
        resp = "{\"ok\":true}";
        return 200;
    }

    private RoomState GetRoom(string gameId)
    {
        if (!rooms.TryGetValue(gameId, out var room))
        {
            room = new RoomState();
            rooms[gameId] = room;
        }
        return room;
    }

    public bool TogglePause(string gameId)
    {
        lock (_lock) { var r = GetRoom(gameId); r.paused = !r.paused; return r.paused; }
    }

    public bool Kick(string gameId, int playerId)
    {
        lock (_lock)
        {
            var r = GetRoom(gameId);
            if (r.players.TryGetValue(playerId, out var ps)) { ps.kicked = true; return true; }
            return false;
        }
    }

    private string BuildStatsJson(string gameId)
    {
        lock (_lock)
        {
            var r = GetRoom(gameId);
            var sb = new StringBuilder();
            sb.Append("{\"paused\":").Append(r.paused ? "true" : "false").Append(",\"players\":[");
            bool first = true;
            foreach (var kv in r.players)
            {
                if (!first) sb.Append(",");
                first = false;
                var id = kv.Key; var ps = kv.Value;
                sb.Append("{\"id\":").Append(id).Append(",\"x\":").Append(ps.data.posX)
                  .Append(",\"y\":").Append(ps.data.posY).Append(",\"z\":").Append(ps.data.posZ)
                  .Append(",\"kicked\":").Append(ps.kicked ? "true" : "false").Append("}");
            }
            sb.Append("]}");
            return sb.ToString();
        }
    }
}
#endif
