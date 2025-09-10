using UnityEngine;
using System.Collections.Generic;

public class BubblePool : MonoBehaviour
{
    [SerializeField] private Bubble _prefab;
    [SerializeField] private int _prewarm = 40;

    private readonly Queue<Bubble> _pool = new Queue<Bubble>();

    void Awake()
    {
        for (int i = 0; i < _prewarm; i++)
        {
            var b = Instantiate(_prefab, transform);
            b.gameObject.SetActive(false);
            _pool.Enqueue(b);
        }
    }

    public Bubble Get()
    {
        var b = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_prefab, transform);
        b.gameObject.SetActive(true);
        return b;
    }

    public void Return(Bubble b)
    {
        if (b == null) return;
        b.gameObject.SetActive(false);
        _pool.Enqueue(b);
    }
}
