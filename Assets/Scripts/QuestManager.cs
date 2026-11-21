using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestState
{
    public QuestData data;
    public bool started;
    public bool completed;
    public int progress;
    public readonly HashSet<DialogData> seenDialogs = new HashSet<DialogData>();
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // (misi, apakah perlu diumumkan di layar)
    public static event Action<QuestData, bool> OnQuestStarted;
    public static event Action<QuestData> OnQuestProgress;
    public static event Action<QuestData> OnQuestCompleted;
    // Semua misi WAJIB di bab itu sudah selesai -> dipakai gate bab (Tahap 8)
    public static event Action<int> OnMandatoryComplete;
    // Ada perubahan apa pun -> UI menyegarkan daftar
    public static event Action OnQuestsChanged;

    [Header("Daftar Misi")]
    [Tooltip("Masukkan SEMUA aset misi (semua bab) ke sini")]
    public QuestData[] allQuests;

    [Tooltip("Bab yang aktif saat game mulai")]
    public int startChapter = 1;

    public int CurrentChapter { get; private set; }
    public IReadOnlyList<QuestState> States => states;

    readonly List<QuestState> states = new List<QuestState>();
    readonly Dictionary<string, QuestState> byId = new Dictionary<string, QuestState>();

    void Awake()
    {
        Instance = this;
        states.Clear();
        byId.Clear();

        if (allQuests == null) return;
        foreach (QuestData q in allQuests)
        {
            if (q == null) continue;
            if (string.IsNullOrEmpty(q.questId))
            {
                Debug.LogWarning("[Quest] Ada misi tanpa questId: " + q.name);
                continue;
            }
            if (byId.ContainsKey(q.questId))
            {
                Debug.LogWarning("[Quest] questId ganda: " + q.questId);
                continue;
            }
            QuestState s = new QuestState { data = q };
            states.Add(s);
            byId[q.questId] = s;
        }
    }

    void OnEnable()
    {
        DialogManager.OnDialogEnded += HandleDialogEnded;
    }

    void OnDisable()
    {
        DialogManager.OnDialogEnded -= HandleDialogEnded;
    }

    void Start()
    {
        StartChapter(startChapter);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- Mulai misi ----------

    // Mulai semua misi sebuah bab (yang tidak menunggu misi lain)
    public void StartChapter(int chapter)
    {
        CurrentChapter = chapter;
        foreach (QuestState s in states)
        {
            if (s.started || s.data.chapter != chapter) continue;

            QuestData pre = s.data.startAfter;
            bool ready = pre == null || IsCompleted(pre.questId);
            if (ready) StartInternal(s, false);
        }
        OnQuestsChanged?.Invoke();
    }

    void StartInternal(QuestState s, bool announce)
    {
        s.started = true;
        Debug.Log("[Quest] Mulai: " + s.data.title);
        OnQuestStarted?.Invoke(s.data, announce);
    }

    // ---------- Progres & selesai ----------

    // Tambah 1 progres. Aman dipanggil dari UnityEvent (1 parameter string).
    public void AddProgress(string questId)
    {
        QuestState s = GetState(questId);
        if (s == null || !s.started || s.completed) return;

        s.progress++;
        Debug.Log("[Quest] " + s.data.title + ": " + s.progress + "/" + s.data.requiredCount);
        OnQuestProgress?.Invoke(s.data);

        if (s.progress >= s.data.requiredCount) CompleteQuest(questId);
        else OnQuestsChanged?.Invoke();
    }

    // Langsung selesaikan misi. Aman dipanggil dari UnityEvent.
    public void CompleteQuest(string questId)
    {
        QuestState s = GetState(questId);
        if (s == null || !s.started || s.completed) return;

        s.completed = true;
        s.progress = Mathf.Max(s.progress, s.data.requiredCount);
        Debug.Log("[Quest] Selesai: " + s.data.title);

        if (s.data.barReward != 0 && GameManager.Instance != null)
            GameManager.Instance.AddBar(s.data.barReward);

        OnQuestCompleted?.Invoke(s.data);

        // Misi lanjutan yang menunggu misi ini
        foreach (QuestState other in states)
        {
            if (other.started || other.data.startAfter != s.data) continue;
            if (other.data.chapter != CurrentChapter) continue;
            StartInternal(other, true);
        }

        if (s.data.mandatory && AreMandatoryComplete(s.data.chapter))
        {
            Debug.Log("[Quest] Semua misi wajib Bab " + s.data.chapter + " selesai");
            OnMandatoryComplete?.Invoke(s.data.chapter);
        }

        OnQuestsChanged?.Invoke();
    }

    // Dialog selesai -> hitung sebagai progres misi yang cocok
    void HandleDialogEnded(DialogData finished)
    {
        if (finished == null) return;

        // Kumpulkan dulu, baru proses (supaya misi lanjutan tidak ikut terhitung di dialog yang sama)
        List<QuestState> matched = new List<QuestState>();
        foreach (QuestState s in states)
        {
            if (!s.started || s.completed) continue;
            DialogData[] list = s.data.progressDialogs;
            if (list == null || Array.IndexOf(list, finished) < 0) continue;
            if (s.seenDialogs.Add(finished)) matched.Add(s);
        }
        foreach (QuestState s in matched) AddProgress(s.data.questId);
    }

    // ---------- Pertanyaan status ----------

    public QuestState GetState(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        byId.TryGetValue(questId, out QuestState s);
        return s;
    }

    public bool IsCompleted(string questId)
    {
        QuestState s = GetState(questId);
        return s != null && s.completed;
    }

    // true jika semua misi wajib di bab itu selesai (dipakai gate bab)
    public bool AreMandatoryComplete(int chapter)
    {
        foreach (QuestState s in states)
            if (s.data.chapter == chapter && s.data.mandatory && !s.completed) return false;
        return true;
    }

    // Target yang ditunjuk panah: misi wajib dulu (urut daftar), lalu opsional.
    // Pada misi dengan banyak target, dipilih yang terdekat dari 'from'.
    public QuestTarget GetCurrentTarget(Vector3 from)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            bool wantMandatory = pass == 0;
            foreach (QuestState s in states)
            {
                if (!s.started || s.completed || s.data.mandatory != wantMandatory) continue;

                QuestTarget best = null;
                float bestDist = float.MaxValue;
                foreach (QuestTarget t in QuestTarget.All)
                {
                    if (t.questId != s.data.questId || !t.IsAvailable) continue;
                    float d = (t.transform.position - from).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = t; }
                }
                if (best != null) return best;
            }
        }
        return null;
    }
}
