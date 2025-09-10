using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Bubble : MonoBehaviour
{
    public BubbleColor Color { get; private set; }
    public BubbleGrid Grid { get; private set; }

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private CircleCollider2D _col;
    private bool _isMoving;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<CircleCollider2D>() ?? gameObject.AddComponent<CircleCollider2D>();
        _col.isTrigger = false;

        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void Initialize(BubbleGrid grid, BubbleColor color)
    {
        Grid = grid;
        SetColor(color);
        if (_col) _col.radius = Grid.BubbleRadius;   // BubbleGrid needs: public float BubbleRadius => bubbleRadius;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.angularVelocity = 0f;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _isMoving = false;
    }

    public void SetColor(BubbleColor color)
    {
        Color = color;
        _sr.color = ColorToRGBA(color);
    }

    public void Launch(Vector2 direction, float speed)
    {
        _isMoving = true;
        _rb.bodyType = RigidbodyType2D.Dynamic;          // keep dynamic while flying
        _rb.linearVelocity = direction.normalized * speed;     // not linearVelocity
        _rb.linearVelocity = direction.normalized * speed;

    }

 void OnCollisionEnter2D(Collision2D col)
{
    if (!_isMoving) return;

    if (col.collider.CompareTag("Bubble") || col.collider.CompareTag("Ceiling"))
    {
        _rb.linearVelocity = Vector2.zero;      // not linearVelocity
        _isMoving = false;

        // Contact point anchors the snap locally near the impact
        Vector2 hit = (col.contactCount > 0) ? col.GetContact(0).point
                                             : (Vector2)transform.position;

        Grid.SnapBubbleAt(this, hit);     // ← use the method you implemented
    }
}


    public void SetStatic()
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.bodyType = RigidbodyType2D.Static;           // snapped grid bubbles = Static
        _rb.linearVelocity = Vector2.zero;

    }

   private static UnityEngine.Color ColorToRGBA(BubbleColor c)
{
    switch (c)
    {
        case BubbleColor.Red:    return new UnityEngine.Color(0.90f, 0.20f, 0.25f);
        case BubbleColor.Green:  return new UnityEngine.Color(0.20f, 0.80f, 0.35f);
        case BubbleColor.Blue:   return new UnityEngine.Color(0.20f, 0.45f, 0.95f);
        case BubbleColor.Yellow: return new UnityEngine.Color(0.98f, 0.85f, 0.20f);
        case BubbleColor.Purple: return new UnityEngine.Color(0.70f, 0.35f, 0.90f);
        case BubbleColor.Orange: return new UnityEngine.Color(0.98f, 0.55f, 0.15f);
        default: return UnityEngine.Color.white;
    }
}

}
