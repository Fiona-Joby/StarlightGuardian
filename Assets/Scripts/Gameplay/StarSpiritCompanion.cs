using System.Collections;
using UnityEngine;

/// <summary>A small animated star spirit that follows the player and can reunite with its friend.</summary>
public class StarSpiritCompanion : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.18f;
    [SerializeField, Min(0.01f)] private float frameDuration = 0.18f;

    private Transform player;
    private Sprite[] moveFrames;
    private SpriteRenderer spriteRenderer;
    private PlayerInput playerInput;
    private Vector3 followVelocity;
    private float animationTimer;
    private bool reuniting;

    public static StarSpiritCompanion Attach(Transform owner, Sprite[] frames)
    {
        if (owner == null || frames == null || frames.Length < 2 || frames[0] == null || frames[1] == null)
            return null;
        StarSpiritCompanion companion = Object.FindAnyObjectByType<StarSpiritCompanion>();
        if (companion == null)
        {
            GameObject companionObject = new GameObject("Star Spirit Companion");
            companion = companionObject.AddComponent<StarSpiritCompanion>();
        }
        companion.Initialize(owner, frames);
        return companion;
    }

    private void Initialize(Transform owner, Sprite[] frames)
    {
        player = owner;
        playerInput = owner.GetComponent<PlayerInput>();
        moveFrames = frames;
        GameObject visual = new GameObject("Star Spirit Companion Art");
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * 0.065f;
        spriteRenderer = visual.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = moveFrames[0];
        spriteRenderer.sortingOrder = 8;
        transform.position = owner.position + new Vector3(-0.8f, 1.1f, 0f);
    }

    private void Update()
    {
        if (player == null || reuniting) return;
        animationTimer += Time.deltaTime;
        spriteRenderer.sprite = moveFrames[Mathf.FloorToInt(animationTimer / frameDuration) % moveFrames.Length];
        float side = playerInput != null && !playerInput.FacingRight ? 0.8f : -0.8f;
        float bob = Mathf.Sin(Time.time * 3f) * 0.12f;
        Vector3 target = player.position + new Vector3(side, 1.1f + bob, 0f);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref followVelocity, followSmoothTime);
    }

    public IEnumerator ReuniteWith(Transform freedSpirit, Sprite companionPose, Sprite freedSpiritPose)
    {
        if (freedSpirit == null || companionPose == null || freedSpiritPose == null) yield break;
        reuniting = true;
        while (freedSpirit != null && Vector3.Distance(transform.position, freedSpirit.position) > 0.12f)
        {
            transform.position = Vector3.MoveTowards(transform.position, freedSpirit.position, 4f * Time.deltaTime);
            yield return null;
        }

        if (freedSpirit == null) yield break;
        Vector3 reunionPosition = (transform.position + freedSpirit.position) * 0.5f;
        spriteRenderer.enabled = false;
        SpriteRenderer freedRenderer = freedSpirit.GetComponent<SpriteRenderer>();
        if (freedRenderer != null) freedRenderer.enabled = false;

        CreateReunitedSpirit("Reunited Star Spirit Companion", companionPose,
            reunionPosition + Vector3.left * 0.95f, false);
        CreateReunitedSpirit("Reunited Freed Star Spirit", freedSpiritPose,
            reunionPosition + Vector3.right * 0.95f, true);
    }

    private static void CreateReunitedSpirit(string name, Sprite pose, Vector3 position, bool flipX)
    {
        GameObject spirit = new GameObject(name);
        spirit.transform.position = position;
        spirit.transform.localScale = Vector3.one * 0.15f;
        SpriteRenderer renderer = spirit.AddComponent<SpriteRenderer>();
        renderer.sprite = pose;
        renderer.flipX = flipX;
        renderer.sortingOrder = 15;
    }
}
