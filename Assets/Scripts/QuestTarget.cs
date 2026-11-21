using System.Collections.Generic;
using UnityEngine;

public class QuestTarget : MonoBehaviour
{
    // Daftar semua target yang aktif (juga dipakai peta mini nanti)
    public static readonly List<QuestTarget> All = new List<QuestTarget>();

    [Tooltip("questId misi yang memakai target ini (harus sama persis dengan di QuestData)")]
    public string questId = "quest_";

    [Tooltip("Tinggi ujung panah dari pivot objek (NPC kapsul: sekitar 1.8)")]
    public float heightOffset = 1.8f;

    [Tooltip("Opsional: setelah dialog ini selesai, target ini tidak ditunjuk lagi (cocok untuk NPC)")]
    public DialogData hideAfterDialog;

    public bool IsAvailable { get; private set; } = true;

    public Vector3 ArrowPosition => transform.position + Vector3.up * heightOffset;

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        DialogManager.OnDialogEnded += HandleDialogEnded;
    }

    void OnDisable()
    {
        All.Remove(this);
        DialogManager.OnDialogEnded -= HandleDialogEnded;
    }

    void HandleDialogEnded(DialogData finished)
    {
        if (hideAfterDialog != null && finished == hideAfterDialog)
            IsAvailable = false;
    }
}
