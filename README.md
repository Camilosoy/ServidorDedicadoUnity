Este es un proyecto para hacer funcionar el servidor dedicado en unty y voy a explicarlo paso a paso:
Nota: puedes crear un servidor en docker y ejecutar los pasos como cliente

Esto para ejecutarlo como servidor
El servidor solo ejecuta la lógica de autoridad y destruye todo lo demás, activándose mediante un flag de consola.
Se inicia la aplicación compilada con el comando: YourGame.exe -batchmode -nographics. Esto establece Application.isBatchMode = true
El script se activa al detectar que Application.isBatchMode es verdadero y omite el resto del juego.
Se llama a apiServer.StartServer(). Esto inicializa el servidor de sockets para que escuche peticiones HTTP de los clientes.
El script destruye explícitamente los componentes de cliente del objeto GameManager (ApiClient.cs, SyncLoop.cs) y destruye el MultiplayerUI y cualquier objeto PlayerController en la escena.
La instancia queda operativa, consumiendo un mínimo de recursos y esperando conexiones en la IP pública.

Esto para ejecutarlo como cliente:
El jugador ejecuta la aplicación sin flags de consola. Esto establece Application.isBatchMode = false.
El script se autodestruye al detectar Application.isBatchMode = false, dejando que la interfaz de usuario tome el control.
El jugador ingresa la IP remota del Servidor Dedicado en el campo de texto y hace clic en "Start As Client".
El método configura la URL del ApiClient para apuntar al Servidor Dedicado (http://IP_REMOTE:PORT/server) e inicializa el localPlayerId.
Llama a sync.StartSync(). El cliente comienza a hacer PUSH (POST de posición) y PULL (GET de todos los estados) del ApiServer remoto.
El script de movimiento LocalInputMover se adjunta al objeto de jugador con el localPlayerId correspondiente, permitiendo el gameplay.

Librerias clave: System.Net, System.Threading, UnityEngine.UI, System.Linq y UnityEngine

Explicacion de los codigos:
ApiServer.cs : AUTORIDAD: Motor que maneja el estado central y las peticiones HTTP (TcpListener/ThreadPool).
DedicatedServerInitializer.cs : INICIALIZADOR: Punto de entrada para el modo headless. Inicia el servidor y destruye toda la lógica de cliente.
MultiplayerUI.cs : INTERFAZ: Permite al usuario configurar IP, Puerto e ID, e iniciar la conexión (StartAsClient) y la desconexión (StopAll).
ApiClient.cs : CLIENTE HTTP: Se encarga de construir y enviar las peticiones POST/GET al ApiServer remoto.
SyncLoop.cs : BUCLE PRINCIPAL: Ejecuta el Coroutine que gestiona el ritmo de las peticiones ApiClient (el tickrate).
LocalInputMover.cs : LÓGICA DE JUEGO: Mueve el personaje del jugador local basándose en la entrada del teclado/controlador.
