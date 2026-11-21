using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class ChapterTitleUI : MonoBehaviour
{
    public TMP_Text titleText;     
    public TMP_Text subtitleText;  

    public float fadeIn = 0.6f;
    public float hold = 2.2f;
    public float fadeOut = 0.9f;

    CanvasGroup group;
    Coroutine routine;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    void OnEnable()
    {
        ChapterManager.OnChapterStarted += Show;
    }

    void OnDisable()
    {
        ChapterManager.OnChapterStarted -= Show;
        routine = null;
    }

    void Show(int chapter)
    {
        string sub = "";
        if (ChapterManager.Instance != null)
        {
            ChapterManager.ChapterInfo info = ChapterManager.Instance.GetInfo(chapter);
            if (info != null) sub = info.title;
        }

        if (titleText != null) titleText.text = "BAB " + chapter;
        if (subtitleText != null) subtitleText.text = sub;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }
        group.alpha = 1f;

        yield return new WaitForSecondsRealtime(hold);

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeOut);
            yield return null;
        }
        group.alpha = 0f;
        routine = null;
    }
}
