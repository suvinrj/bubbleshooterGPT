using UnityEngine;
using System.Collections.Generic;

public class BubbleGrid : MonoBehaviour
{
    [Header("Grid Dimensions")]
    [SerializeField] private int rows = 9;
    [SerializeField] private int columns = 10;
    [SerializeField] private float bubbleRadius = 0.5f; // must match prefab radius
    [SerializeField] private int startFilledRows = 5;

    public float BubbleRadius => bubbleRadius;  // ← add this


    [Header("References")]
    [SerializeField] private BubblePool pool;
    [SerializeField] private Transform gridRoot; // optional parent for placed bubbles

Transform Parent => gridRoot ? gridRoot : transform;
Vector3 WorldPos(Vector2 local) => Parent.TransformPoint(local);


    // Internal structure
    private class Node
    {
        public int id;
        public int r, c;
        public Vector2 pos;
        public Bubble occupant; // null if empty
        public readonly List<int> neighbors = new List<int>();
    }

    private readonly List<Node> _nodes = new List<Node>();
    private float _xStep, _yStep, _oddXOffset;

    // Fast lookup: (r,c) -> index (id)
    private int IndexOf(int r, int c) => r * columns + c;

    void Start()
    {
        BuildGrid();
        BuildAdjacency();
        SeedBoard();
    }

    void BuildGrid()
    {
        _nodes.Clear();
        _xStep = bubbleRadius * 2f;                // distance between centers in a row
        _yStep = Mathf.Sqrt(3f) * bubbleRadius;    // hex vertical spacing
        _oddXOffset = bubbleRadius;                // odd rows are shifted by +R

        int id = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = c * _xStep + ((r % 2 == 1) ? _oddXOffset : 0f);
                float y = -r * _yStep; // top row r=0 at y≈0, grows downward
                var n = new Node { id = id++, r = r, c = c, pos = new Vector2(x, y) };
                _nodes.Add(n);
            }
        }

        // Center the grid around x=0
        float maxWidth = (columns - 1) * _xStep + _oddXOffset;
        float xCenterShift = -maxWidth / 2f;
        foreach (var n in _nodes)
            n.pos.x += xCenterShift;
    }

    void BuildAdjacency()
    {
        float neighborDist = bubbleRadius * 2f;
        float eps = neighborDist * 0.15f; // tolerance

        for (int i = 0; i < _nodes.Count; i++)
        {
            var a = _nodes[i];
            a.neighbors.Clear();
            for (int j = 0; j < _nodes.Count; j++)
            {
                if (i == j) continue;
                var b = _nodes[j];
                float d = Vector2.Distance(a.pos, b.pos);
                if (Mathf.Abs(d - neighborDist) <= eps)
                    a.neighbors.Add(j);
            }
        }
    }

    void SeedBoard()
    {
        System.Array colors = System.Enum.GetValues(typeof(BubbleColor));
        for (int r = 0; r < Mathf.Min(startFilledRows, rows); r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int id = IndexOf(r, c);
                var node = _nodes[id];
                var bubble = pool.Get();
bubble.transform.SetParent(Parent, false);
bubble.transform.localPosition = node.pos;       // respect grid transform
                bubble.Initialize(this, (BubbleColor)colors.GetValue(Random.Range(0, colors.Length)));
                bubble.SetStatic();
                node.occupant = bubble;
            }
        }
    }

    public IReadOnlyList<BubbleColor> ColorsOnBoard()
    {
        var set = new HashSet<BubbleColor>();
        foreach (var n in _nodes)
            if (n.occupant != null) set.Add(n.occupant.Color);
        if (set.Count == 0) // fallback
        {
            foreach (BubbleColor c in System.Enum.GetValues(typeof(BubbleColor)))
                set.Add(c);
        }
        return new List<BubbleColor>(set);
    }

    public void SnapBubble(Bubble bubble)
    {
        // Find nearest empty node
        int best = -1;
float bestSqr = float.MaxValue;
for (int i = 0; i < _nodes.Count; i++)
{
    if (_nodes[i].occupant != null) continue;
    Vector3 nodeWorld = WorldPos(_nodes[i].pos);
    float d2 = (bubble.transform.position - nodeWorld).sqrMagnitude;
    if (d2 < bestSqr) { bestSqr = d2; best = i; }
}

        if (best < 0)
        {
            // Grid is full; just return bubble to pool
            pool.Return(bubble);
            return;
        }

        var target = _nodes[best];
bubble.transform.SetParent(Parent, false);
bubble.transform.localPosition = target.pos;
bubble.SetStatic();
target.occupant = bubble;

        // Resolve matches and drops
        ResolveMatchesAndDrops(best);
    }


public void SnapBubbleAt(Bubble bubble, Vector2 hitPoint)
{
    int best = -1;
    float bestSqr = float.MaxValue;

    float maxAttachDist = bubbleRadius * 1.4f; // only consider nodes near the impact

    for (int i = 0; i < _nodes.Count; i++)
    {
        var n = _nodes[i];
        if (n.occupant != null) continue;

        // Must be close to where we actually hit
        if (Vector2.Distance(WorldPos(n.pos), hitPoint) > maxAttachDist) continue;

        // Must be an attachable spot: top row or touching at least one occupied neighbor
        bool attachable = (n.r == 0);
        if (!attachable)
        {
            foreach (int nei in n.neighbors)
            {
                if (_nodes[nei].occupant != null) { attachable = true; break; }
            }
        }
        if (!attachable) continue;

        // Choose the closest among candidates
        float d2 = (bubble.transform.position - WorldPos(n.pos)).sqrMagnitude;
        if (d2 < bestSqr) { bestSqr = d2; best = i; }
    }

    if (best < 0)
    {
        // Fallback: old behavior (rare, e.g., grazing hits)
        SnapBubble(bubble);
        return;
    }

    PlaceBubbleAtNode(bubble, best);
}

// factor out the placement so both paths use the same logic
void PlaceBubbleAtNode(Bubble bubble, int index)
{
    var target = _nodes[index];
    bubble.transform.SetParent(Parent, false);
    bubble.transform.localPosition = target.pos;
    bubble.SetStatic();
    target.occupant = bubble;

    ResolveMatchesAndDrops(index);
}



    void ResolveMatchesAndDrops(int placedIndex)
    {
        // 1) Same‑color cluster
        var placedNode = _nodes[placedIndex];
        if (placedNode.occupant == null) return;
        var color = placedNode.occupant.Color;

        var same = CollectCluster(placedIndex, n => n.occupant != null && n.occupant.Color == color);
        if (same.Count >= 3)
        {
            // Pop matched cluster
            foreach (int i in same)
            {
                var n = _nodes[i];
                pool.Return(n.occupant);
                n.occupant = null;
            }

            // 2) Floating clusters: keep only those connected to top row
            var connectedToTop = new bool[_nodes.Count];
            var queue = new Queue<int>();
            for (int c = 0; c < columns; c++)
            {
                int topIdx = IndexOf(0, c);
                if (_nodes[topIdx].occupant != null)
                {
                    connectedToTop[topIdx] = true;
                    queue.Enqueue(topIdx);
                }
            }
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                foreach (int nei in _nodes[cur].neighbors)
                {
                    if (connectedToTop[nei]) continue;
                    if (_nodes[nei].occupant == null) continue;
                    connectedToTop[nei] = true;
                    queue.Enqueue(nei);
                }
            }

            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].occupant != null && !connectedToTop[i])
                {
                    // Drop (for simplicity, just remove)
                    pool.Return(_nodes[i].occupant);
                    _nodes[i].occupant = null;
                }
            }
        }
    }

  // Collect a contiguous cluster starting at startIndex, but ONLY walking through nodes that satisfy `rule`.
System.Collections.Generic.List<int> CollectCluster(int startIndex, System.Predicate<Node> rule)
{
    var visited = new bool[_nodes.Count];
    var stack = new System.Collections.Generic.Stack<int>();
    var result = new System.Collections.Generic.List<int>();

    stack.Push(startIndex);
    visited[startIndex] = true;

    while (stack.Count > 0)
    {
        int cur = stack.Pop();
        var n = _nodes[cur];

        if (!rule(n))
            continue;

        result.Add(cur);

        // Only expand into neighbors that also satisfy the rule (prevents crossing gaps/mismatched colors)
        foreach (int nei in n.neighbors)
        {
            if (visited[nei]) continue;

            var nn = _nodes[nei];
            if (!rule(nn)) continue;        // <— key line

            visited[nei] = true;
            stack.Push(nei);
        }
    }
    return result;
}
}
