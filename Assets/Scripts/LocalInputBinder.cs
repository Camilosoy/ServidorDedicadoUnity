using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class LocalInputBinder : MonoBehaviour
{
    private void Start()
    {
        var pc = GetComponent<PlayerController>();
        var sync = FindObjectOfType<SyncLoop>();
        if (!pc || !sync) return;

        // Si este objeto es el jugador local, añade el LocalInputMover; si no, asegúrate de no tenerlo
        if (pc.GetPlayerId() == sync.localPlayerId)
        {
            if (GetComponent<LocalInputMover>() == null)
            {
                var mover = gameObject.AddComponent<LocalInputMover>();
                mover.speed = 5f;
            }
            pc.isLocal = true;
        }
        else
        {
            var mover = GetComponent<LocalInputMover>();
            if (mover) Destroy(mover);
            pc.isLocal = false;
        }
    }
}