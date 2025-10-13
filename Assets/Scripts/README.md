# Multiplayer Player-Host (Unity 6, .NET Standard 2.1) — FIXED

**Qué hay aquí:** Pack final sin `LocalInputBinder`. La UI decide el `LocalId` y activa/quita `LocalInputMover` con `GameManager.RebindLocalControl()`. Servidor `TcpListener` **dual-stack** (IPv4+IPv6) y apagado limpio.

## Montaje rápido
1. Arrastra estos scripts a `Assets/Scripts/`.
2. Escena:
   - 3 cápsulas `Player`, `Player (1)`, `Player (2)` con **PlayerController** (ids 0,1,2). **No** uses LocalInputBinder.
   - GO `GameManager`: añade **ApiClient**, **GameManager**, **SyncLoop**. En `GameManager.players` arrastra las cápsulas en orden 0..N.
   - **Canvas** con `MultiplayerUI` y dos paneles:
     - `DisconnectedPanel` (activo) con 4 **Input Field (Legacy)**: IP, Port, GameId, LocalId; y botones **Start Host** y **Start Client**.
     - `ConnectedPanel` (inactivo) con botón **Stop**. (Opcional) añade `HostPanel` con **ApiServer** y `HostControls`.
   - **EventSystem** con **Standalone Input Module**.
3. Prueba:
   - **Editor (Host):** IP 127.0.0.1, Port 5005, GameId room-1, LocalId 0 → Start Host.
   - **Build (Cliente):** IP 127.0.0.1, Port 5005, GameId room-1, LocalId 1 → Start Client.

## Señales de OK
- Consola del host: `[ApiServer] TcpListener escuchando en http://0.0.0.0:5005/ (dual-stack)`
- Navegador del host: `http://127.0.0.1:5005/server/room-1/0` devuelve JSON.
- Cada ventana mueve **solo su** cápsula; ambas se ven con suavizado.