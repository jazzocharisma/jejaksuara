using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("Deteksi")]
    public float interactRadius = 2.5f;

    [Header("UI Prompt")]
    [Tooltip("Objek induk prompt (dihidup/matikan)")]
    public GameObject promptRoot;
    [Tooltip("Teks TextMeshPro di dalam prompt")]
    public TMP_Text promptLabel;

    [Header("Kontrol")]
    // Nanti diset false saat dialog / inventaris / pause
    public bool interactionEnabled = true;

    Interactable current;
    readonly Collider[] hits = new Collider[16];

    void Start()
    {
        HidePrompt();
    }

    void Update()
    {
        if (!interactionEnabled)
        {
            SetCurrent(null);
            return;
        }

        SetCurrent(FindNearest());

        if (current != null && InteractPressed())
        {
            current.Interact();
        }
    }

    Interactable FindNearest()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, interactRadius, hits, ~0, QueryTriggerInteraction.Collide);

        Interactable best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Interactable it = hits[i].GetComponentInParent<Interactable>();
            if (it == null || !it.canInteract) continue;

            float d = (it.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = it;
            }
        }
        return best;
    }

    bool InteractPressed()
    {
        // Hanya saat cursor terkunci (mode gameplay), supaya klik UI tidak ikut memicu
        if (Cursor.lockState != CursorLockMode.Locked) return false;

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool e = kb != null && kb.eKey.wasPressedThisFrame;
        bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;
        return e || click;
    }

    void SetCurrent(Interactable next)
    {
        if (next == current) return;
        current = next;

        if (current == null) HidePrompt();
        else ShowPrompt(current.promptText);
    }

    void ShowPrompt(string text)
    {
        if (promptLabel != null) promptLabel.text = "[E] " + text;
        if (promptRoot != null) promptRoot.SetActive(true);
    }

    void HidePrompt()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
    }
}
