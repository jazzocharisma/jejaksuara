using System.Collections;
using UnityEngine;

public class ChapterGate : MonoBehaviour
{
    [Tooltip("Gerbang ini menutup jalan KELUAR dari bab nomor berapa (Gate-bab1 = 1, Gate-bab2 = 2, ...)")]
    public int chapter = 1;

    [Tooltip("Lama animasi gerbang turun (detik)")]
    public float openDuration = 1.2f;

    [Tooltip("Kedalaman gerbang turun. 0 = otomatis setinggi gerbang")]
    public float sinkDepth = 0f;

    public bool IsOpen { get; private set; }

    Collider[] cols;

    void Awake()
    {
        cols = GetComponentsInChildren<Collider>();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        // Collider dimatikan dulu supaya pemain langsung bisa lewat
        foreach (Collider c in cols) c.enabled = false;
        StartCoroutine(Sink());
    }

    IEnumerator Sink()
    {
        float depth = sinkDepth;
        if (depth <= 0f)
        {
            Renderer r = GetComponentInChildren<Renderer>();
            depth = (r != null ? r.bounds.size.y : 4f) + 0.5f;
        }

        Vector3 from = transform.position;
        Vector3 to = from + Vector3.down * depth;

        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / openDuration));
            yield return null;
        }

        gameObject.SetActive(false);
    }

    // Jarak titik ke gerbang (dipakai untuk petunjuk "gerbang terkunci")
    public float DistanceTo(Vector3 point)
    {
        if (IsOpen) return float.MaxValue;

        foreach (Collider c in cols)
        {
            if (c.enabled) return Vector3.Distance(c.ClosestPoint(point), point);
        }
        return Vector3.Distance(transform.position, point);
    }
}
