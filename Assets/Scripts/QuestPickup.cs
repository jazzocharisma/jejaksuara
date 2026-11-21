using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class QuestPickup : MonoBehaviour
{
    [Tooltip("questId misi yang dipengaruhi")]
    public string questId = "quest_";

    [Tooltip("Centang = langsung menyelesaikan misi. Kosongkan = hanya +1 progres")]
    public bool completeDirectly = false;

    [Tooltip("Objek disembunyikan setelah diambil")]
    public bool disappearAfter = true;

    void Awake()
    {
        GetComponent<Interactable>().onInteract.AddListener(OnInteract);
    }

    void OnInteract()
    {
        if (QuestManager.Instance != null)
        {
            if (completeDirectly) QuestManager.Instance.CompleteQuest(questId);
            else QuestManager.Instance.AddProgress(questId);
        }
        if (disappearAfter) gameObject.SetActive(false);
    }
}
