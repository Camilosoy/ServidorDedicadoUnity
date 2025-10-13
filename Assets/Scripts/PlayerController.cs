using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private int playerId = 0;

    [Tooltip("Si este jugador es el local (controlado con WASD).")]
    public bool isLocal = false;

    [Header("Suavizado (remoto)")]
    [SerializeField] private float lerpPosSpeed = 12f;

    private Vector3 targetPosition;

    private void Awake()
    {
        targetPosition = transform.position;
    }

    private void Update()
    {
        if (!isLocal)
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpPosSpeed);
    }

    public void MovePlayer(Vector3 position)
    {
        if (isLocal) return; // no forzar al local
        targetPosition = position;
    }

    public Vector3 GetPosition() => transform.position;
    public int GetPlayerId() => playerId;
    public void SetPlayerId(int id) => playerId = id;
}
