using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class QuestArrow : MonoBehaviour
{
    [Header("Referensi")]
    public Transform player;
    [Tooltip("Material kuning (shader Universal Render Pipeline/Unlit)")]
    public Material arrowMaterial;

    [Header("Gerak")]
    public float bobHeight = 0.25f;
    public float bobSpeed = 3f;
    public float spinSpeed = 120f;

    [Header("Ukuran (membesar jika target jauh agar tetap terlihat)")]
    public float minScale = 1f;
    public float maxScale = 3.5f;
    public float scalePerMeter = 0.05f;

    [Header("Bentuk")]
    [Range(3, 16)] public int segments = 6;

    MeshRenderer rend;

    void Awake()
    {
        GetComponent<MeshFilter>().sharedMesh = BuildArrowMesh();

        rend = GetComponent<MeshRenderer>();
        if (arrowMaterial != null) rend.sharedMaterial = arrowMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.enabled = false;
    }

    void LateUpdate()
    {
        QuestTarget target = null;
        if (QuestManager.Instance != null && player != null)
            target = QuestManager.Instance.GetCurrentTarget(player.position);

        // Sembunyikan saat tidak ada tujuan atau sedang dialog
        bool show = target != null && !DialogManager.IsOpen;
        rend.enabled = show;
        if (!show) return;

        float dist = Vector3.Distance(player.position, target.transform.position);
        float s = Mathf.Clamp(dist * scalePerMeter, minScale, maxScale);

        // Ujung panah (titik pivot) ada di bawah, menunjuk ke tujuan
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = target.ArrowPosition + Vector3.up * bob;
        transform.rotation = Quaternion.Euler(0f, Time.time * spinSpeed, 0f);
        transform.localScale = Vector3.one * s;
    }

    // Panah menunjuk ke bawah: kerucut di bawah + batang di atas. Ujung = titik (0,0,0).
    Mesh BuildArrowMesh()
    {
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();

        const float coneH = 0.8f, coneR = 0.45f, shaftR = 0.16f, shaftH = 0.6f;
        float y1 = coneH;
        float y2 = coneH + shaftH;
        Vector3 topCenter = new Vector3(0f, y2, 0f);

        for (int i = 0; i < segments; i++)
        {
            float a0 = (float)i / segments * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;

            Vector3 c0 = new Vector3(Mathf.Cos(a0) * coneR, y1, Mathf.Sin(a0) * coneR);
            Vector3 c1 = new Vector3(Mathf.Cos(a1) * coneR, y1, Mathf.Sin(a1) * coneR);
            Vector3 lo0 = new Vector3(Mathf.Cos(a0) * shaftR, y1, Mathf.Sin(a0) * shaftR);
            Vector3 lo1 = new Vector3(Mathf.Cos(a1) * shaftR, y1, Mathf.Sin(a1) * shaftR);
            Vector3 hi0 = new Vector3(lo0.x, y2, lo0.z);
            Vector3 hi1 = new Vector3(lo1.x, y2, lo1.z);

            // Sisi kerucut
            AddTri(v, t, Vector3.zero, c0, c1);
            // Cincin atas kerucut (menghadap atas)
            AddTri(v, t, lo0, c1, c0);
            AddTri(v, t, lo0, lo1, c1);
            // Sisi batang
            AddTri(v, t, lo0, hi0, hi1);
            AddTri(v, t, lo0, hi1, lo1);
            // Tutup atas batang
            AddTri(v, t, topCenter, hi1, hi0);
        }

        Mesh m = new Mesh { name = "QuestArrowMesh" };
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static void AddTri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
    }
}
