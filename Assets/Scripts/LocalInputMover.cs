using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class LocalInputMover : MonoBehaviour
{
    public float speed = 5f;
    private PlayerController pc;

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        pc.isLocal = true;
    }

    private void Update()
    {
        Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }
}
