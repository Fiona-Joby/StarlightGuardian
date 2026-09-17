using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float movement = Input.GetAxis("Horizontal");

        transform.Translate(
            Vector2.right * movement * moveSpeed * Time.deltaTime
        );

        Vector3 position = transform.position;

        position.x = Mathf.Clamp(position.x, -8f, 8f);

        transform.position = position;
    }
}