using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class DialogNPC : MonoBehaviour
{
    [Tooltip("ID unik NPC, dipakai untuk menyimpan hubungan (contoh: koordinator)")]
    public string npcId = "npc_1";

    public DialogData dialog;

    [Tooltip("Opsional: dialog singkat yang dipakai jika dialog utama sudah pernah selesai " +
             "(contoh: 'Ayo bersiap, kita segera berangkat'). Kosong = dialog utama diulang tanpa efek bar.")]
    public DialogData repeatDialog;

    [Tooltip("Titik wajah NPC untuk kamera dialog (objek kosong anak NPC). Boleh dikosongkan.")]
    public Transform faceTarget;

    public Vector3 FacePoint
    {
        get
        {
            if (faceTarget != null) return faceTarget.position;
            return transform.position + Vector3.up * 1.6f;
        }
    }

    void Awake()
    {
        // Otomatis tersambung ke tombol interaksi (E)
        GetComponent<Interactable>().onInteract.AddListener(StartDialog);
    }

    public void StartDialog()
    {
        if (dialog == null || DialogManager.Instance == null) return;

        // Sudah pernah bicara? Pakai dialog ulang jika ada
        DialogData toPlay = dialog;
        if (repeatDialog != null && DialogManager.Instance.HasCompleted(dialog))
            toPlay = repeatDialog;

        DialogManager.Instance.StartDialog(toPlay, this);
    }
}