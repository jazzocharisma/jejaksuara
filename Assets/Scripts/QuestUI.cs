using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class QuestUI : MonoBehaviour
{
    [Header("Daftar Misi")]
    public TMP_Text listText;

    [Header("Notifikasi (teks di tengah atas, di bawah bar)")]
    public TMP_Text notifText;
    public Color notifColor = new Color(1f, 0.84f, 0.2f);

    [Header("Warna daftar (hex tanpa #)")]
    public string mandatoryHex = "FFD633";
    public string optionalHex = "FFFFFF";
    public string doneHex = "8C8C8C";

    CanvasGroup group;
    Coroutine notifRoutine;
    readonly Queue<string> queue = new Queue<string>();
    readonly StringBuilder sb = new StringBuilder();

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        QuestManager.OnQuestsChanged += Refresh;
        QuestManager.OnQuestStarted += HandleStarted;
        QuestManager.OnQuestCompleted += HandleCompleted;
        ChapterManager.OnGateOpened += HandleGateOpened;
    }

    void OnDisable()
    {
        QuestManager.OnQuestsChanged -= Refresh;
        QuestManager.OnQuestStarted -= HandleStarted;
        QuestManager.OnQuestCompleted -= HandleCompleted;
        ChapterManager.OnGateOpened -= HandleGateOpened;

        queue.Clear();
        notifRoutine = null;
    }

    void Start()
    {
        if (notifText != null) notifText.alpha = 0f;
        Refresh();
    }

    void Update()
    {
        bool hide = DialogManager.IsOpen
                    || (GameManager.Instance != null && GameManager.Instance.IsGameOver);
        group.alpha = hide ? 0f : 1f;
    }

    void HandleStarted(QuestData q, bool announce)
    {
        if (announce) Notify("Misi baru: " + q.title);
    }

    void HandleCompleted(QuestData q)
    {
        Notify("Misi selesai: " + q.title);
    }

    void HandleGateOpened(int fromChapter)
    {
        Notify("Gerbang menuju Bab " + (fromChapter + 1) + " terbuka!");
    }

    void Refresh()
    {
        if (listText == null || QuestManager.Instance == null) return;

        sb.Clear();
        sb.AppendLine("<b>MISI</b>");

        int shown = 0;
        foreach (QuestState s in QuestManager.Instance.States)
        {
            if (!s.started || s.data.chapter != QuestManager.Instance.CurrentChapter) continue;
            shown++;

            string progress = s.data.requiredCount > 1
                ? " (" + Mathf.Min(s.progress, s.data.requiredCount) + "/" + s.data.requiredCount + ")"
                : "";

            if (s.completed)
            {
                sb.AppendLine("<color=#" + doneHex + "><s>- " + s.data.title + progress + "</s></color>");
            }
            else
            {
                string hex = s.data.mandatory ? mandatoryHex : optionalHex;
                string tag = s.data.mandatory ? "" : " (opsional)";
                sb.AppendLine("<color=#" + hex + ">- " + s.data.title + progress + tag + "</color>");
            }
        }

        if (shown == 0) sb.AppendLine("<color=#" + doneHex + ">Tidak ada misi aktif</color>");
        listText.text = sb.ToString().TrimEnd();
    }

    // Notifikasi masuk antrean, jadi pesan beruntun tidak saling menimpa
    void Notify(string message)
    {
        if (notifText == null) return;
        queue.Enqueue(message);
        if (notifRoutine == null) notifRoutine = StartCoroutine(NotifLoop());
    }

    IEnumerator NotifLoop()
    {
        while (queue.Count > 0)
        {
            notifText.text = queue.Dequeue();
            Color c = notifColor;
            c.a = 1f;
            notifText.color = c;

            // Jika masih ada antrean, tampil lebih singkat
            bool more = queue.Count > 0;
            yield return new WaitForSecondsRealtime(more ? 1.1f : 2.2f);

            float fade = more ? 0.15f : 0.6f;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                c.a = 1f - Mathf.Clamp01(t / fade);
                notifText.color = c;
                yield return null;
            }
        }

        notifText.alpha = 0f;
        notifRoutine = null;
    }
}