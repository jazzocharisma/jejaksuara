using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ChapterTrigger : MonoBehaviour
{
    [Tooltip("Bab yang dimulai saat pemain masuk zona ini")]
    public int chapter = 2;

    void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (ChapterManager.Instance != null) ChapterManager.Instance.EnterChapter(chapter);
    }
}
