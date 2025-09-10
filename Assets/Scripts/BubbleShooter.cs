using UnityEngine;
using System.Linq;

public class BubbleShooter : MonoBehaviour
{
    [SerializeField] private BubblePool pool;
    [SerializeField] private BubbleGrid grid;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float launchSpeed = 18f;
    [SerializeField] private float minAimDeg = 15f;   // clamp so you can't shoot downward
    [SerializeField] private float maxAimDeg = 165f;

    private Bubble _loaded;

    void Start()
    {
        LoadNext();
    }

    void Update()
    {
        if (_loaded == null) return;

        Vector3 m3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
Vector3 diff3 = m3 - launchPoint.position;
diff3.z = 0f; // ignore depth for 2D
Vector2 dir = new Vector2(diff3.x, diff3.y);

        float ang = Vector2.SignedAngle(Vector2.right, dir);
        ang = Mathf.Clamp(ang, minAimDeg, maxAimDeg);
        Vector2 shotDir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

        // Rotate the shooter to face aim (optional visual)
        transform.right = shotDir;

        if (Input.GetMouseButtonDown(0))
        {
            _loaded.transform.position = launchPoint.position;
            _loaded.Launch(shotDir, launchSpeed);
            _loaded = null;
            Invoke(nameof(LoadNext), 0.15f); // slight delay so snap can occur first
        }
    }

    void LoadNext()
    {
        if (_loaded != null) return;
        var b = pool.Get();
        b.transform.position = launchPoint.position;
        var available = grid.ColorsOnBoard();
        var color = available[Random.Range(0, available.Count)];
        b.Initialize(grid, color);
        _loaded = b;
    }
}
