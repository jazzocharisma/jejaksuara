using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ChapterManager : MonoBehaviour
{
    public static ChapterManager Instance { get; private set; }

    // Nomor bab yang baru dimulai
    public static event Action<int> OnChapterStarted;
    // Gerbang yang menutup jalan keluar dari bab ini baru saja terbuka
    public static event Action<int> OnGateOpened;

    [Serializable]
    public class ChapterInfo
    {
        public int number;
        public string title;
    }

    [Header("Bab")]
    public int startChapter = 1;
    public ChapterInfo[] chapters = new ChapterInfo[]
    {
        new ChapterInfo { number = 1, title = "Berkumpul di Kampus" },
        new ChapterInfo { number = 2, title = "Persiapan Aksi" },
        new ChapterInfo { number = 3, title = "Perjalanan" },
        new ChapterInfo { number = 4, title = "Lokasi Aksi" },
    };

    [Header("Gerbang")]
    [Tooltip("Seret Gate-bab1, Gate-bab2, Gate-bab3 (yang sudah dipasangi ChapterGate)")]
    public ChapterGate[] gates;

    [Tooltip("Sementara untuk pengembangan: bab tanpa misi wajib langsung membuka gerbangnya. " +
             "Matikan setelah semua bab punya misi wajib.")]
    public bool autoOpenIfNoMandatory = true;

    [Header("Referensi")]
    public Transform player;

    [Header("Petunjuk gerbang terkunci")]
    public TMP_Text hintText;
    public float hintDistance = 4f;
    public string hintMessage = "Selesaikan semua misi wajib untuk membuka gerbang";

    [Header("Debug (matikan sebelum build final)")]
    [Tooltip("F5 = buka gerbang bab yang sedang aktif")]
    public bool debugKeys = true;

    public int CurrentChapter { get; private set; }

    bool hintVisible;

    void Awake()
    {
        Instance = this;
        CurrentChapter = 0;
    }

    void OnEnable()
    {
        QuestManager.OnMandatoryComplete += HandleMandatoryComplete;
    }

    void OnDisable()
    {
        QuestManager.OnMandatoryComplete -= HandleMandatoryComplete;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (hintText != null) hintText.gameObject.SetActive(false);
        hintVisible = false;

        EnterInternal(startChapter, true);
    }

    void Update()
    {
        if (debugKeys && Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            OpenGate(CurrentChapter);

        UpdateHint();
    }

    // ---------- Masuk bab ----------

    // Dipanggil ChapterTrigger saat pemain memasuki zona bab berikutnya
    public void EnterChapter(int chapter)
    {
        EnterInternal(chapter, false);
    }

    void EnterInternal(int chapter, bool force)
    {
        if (!force)
        {
            if (chapter <= CurrentChapter) return;                       // bab lama / sama: abaikan
            if (chapter > 1 && !IsGateOpen(chapter - 1)) return;         // gerbang belum terbuka
        }

        CurrentChapter = chapter;
        Debug.Log("[Chapter] Masuk Bab " + chapter);

        if (QuestManager.Instance != null) QuestManager.Instance.StartChapter(chapter);
        OnChapterStarted?.Invoke(chapter);

        if (autoOpenIfNoMandatory && !HasMandatoryQuests(chapter))
        {
            Debug.Log("[Chapter] Bab " + chapter + " belum punya misi wajib, gerbang dibuka otomatis");
            OpenGate(chapter);
        }
    }

    // ---------- Gerbang ----------

    void HandleMandatoryComplete(int chapter)
    {
        OpenGate(chapter);
    }

    // Buka gerbang yang menutup jalan keluar dari bab 'fromChapter'
    public void OpenGate(int fromChapter)
    {
        ChapterGate g = FindGate(fromChapter);
        if (g == null || g.IsOpen) return;

        g.Open();
        Debug.Log("[Chapter] Gerbang Bab " + fromChapter + " terbuka");
        OnGateOpened?.Invoke(fromChapter);
    }

    public bool IsGateOpen(int fromChapter)
    {
        ChapterGate g = FindGate(fromChapter);
        return g == null || g.IsOpen; // tidak ada gerbang = bebas
    }

    ChapterGate FindGate(int fromChapter)
    {
        if (gates == null) return null;
        foreach (ChapterGate g in gates)
            if (g != null && g.chapter == fromChapter) return g;
        return null;
    }

    bool HasMandatoryQuests(int chapter)
    {
        if (QuestManager.Instance == null) return true;
        foreach (QuestState s in QuestManager.Instance.States)
            if (s.data.chapter == chapter && s.data.mandatory) return true;
        return false;
    }

    // ---------- Petunjuk ----------

    void UpdateHint()
    {
        if (hintText == null) return;

        bool show = false;
        if (player != null && !DialogManager.IsOpen)
        {
            ChapterGate g = FindGate(CurrentChapter);
            if (g != null && !g.IsOpen && g.DistanceTo(player.position) < hintDistance)
                show = true;
        }

        if (show == hintVisible) return;
        hintVisible = show;
        hintText.gameObject.SetActive(show);
        if (show) hintText.text = hintMessage;
    }

    public ChapterInfo GetInfo(int chapter)
    {
        if (chapters == null) return null;
        foreach (ChapterInfo c in chapters)
            if (c != null && c.number == chapter) return c;
        return null;
    }
}
