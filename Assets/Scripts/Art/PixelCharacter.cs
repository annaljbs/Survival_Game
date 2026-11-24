using UnityEngine;

/// <summary>
/// Walking animation for the player from a 16x16 character sheet (columns = down, up, left, right;
/// rows 0-3 = the four walking frames). Turns the character towards the way it moves.
/// </summary>
[DefaultExecutionOrder(-150)]
[RequireComponent(typeof(SpriteRenderer))]
public class PixelCharacter : MonoBehaviour
{
    [Tooltip("Path inside Assets/Resources, without .png")]
    public string resource = "Pixel/Characters/Hunter";
    public float secondsPerFrame = 0.14f;
    [Tooltip("Height of the character in world units (1 = one tile).")]
    public float size = 1f;

    SpriteRenderer sr;
    readonly Sprite[,] frames = new Sprite[4, 4];
    Vector3 lastPos;
    int direction;      // 0 down, 1 up, 2 left, 3 right
    float timer;
    float lastMoveTime = -10f;
    Vector2 smoothedVelocity;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        for (int d = 0; d < 4; d++)
            for (int f = 0; f < 4; f++)
                frames[d, f] = PixelArt.Get(resource, d * 16, f * 16, 16, 16);

        if (frames[0, 0] == null) { enabled = false; return; }   // picture missing: keep the coloured square

        // Keep the collider the same size in the world while the sprite scale changes
        var circle = GetComponent<CircleCollider2D>();
        float worldRadius = circle != null ? circle.radius * transform.localScale.x : 0f;
        sr.color = Color.white;
        sr.sprite = frames[0, 0];
        transform.localScale = new Vector3(size, size, 1f);
        if (circle != null) circle.radius = worldRadius / size;
        lastPos = transform.position;
    }

    void Update()
    {
        Vector3 delta = transform.position - lastPos;
        lastPos = transform.position;

        // "Moving" stays true for a moment after the last movement, so a single still frame
        // (physics steps do not line up with the screen refresh) does not flicker the walk cycle.
        if (delta.sqrMagnitude > 0.000001f)
        {
            lastMoveTime = Time.time;

            // Which way is the character facing? Use the average movement of the last few frames, and only switch
            // between "sideways" and "up/down" when the other axis is clearly stronger. Walking exactly diagonally
            // used to flip the picture every frame.
            Vector2 velocity = new Vector2(delta.x, delta.y) / Mathf.Max(Time.deltaTime, 0.0001f);
            smoothedVelocity = Vector2.Lerp(smoothedVelocity, velocity, 1f - Mathf.Exp(-10f * Time.deltaTime));
            float ax = Mathf.Abs(smoothedVelocity.x), ay = Mathf.Abs(smoothedVelocity.y);
            bool facingSideways = direction >= 2;
            const float margin = 1.4f;
            if (facingSideways && ay > ax * margin) direction = smoothedVelocity.y > 0f ? 1 : 0;
            else if (!facingSideways && ax > ay * margin) direction = smoothedVelocity.x < 0f ? 2 : 3;
            else if (facingSideways) direction = smoothedVelocity.x < 0f ? 2 : 3;     // keep sideways, but left/right can change
            else direction = smoothedVelocity.y > 0f ? 1 : 0;                           // keep up/down, but up/down can change
        }
        else
        {
            smoothedVelocity = Vector2.zero;
        }
        int frame = 0;
        if (Time.time - lastMoveTime < 0.1f)
        {
            timer += Time.deltaTime;
            frame = (int)(timer / secondsPerFrame) % 4;
        }
        else
        {
            timer = 0f;
        }
        sr.sprite = frames[direction, frame];
    }
}
