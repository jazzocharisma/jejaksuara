using UnityEngine;

[CreateAssetMenu(fileName = "Dialog_", menuName = "Jejak Suara/Dialog")]
public class DialogData : ScriptableObject
{
    public string npcName = "NPC";

    [Tooltip("Centang = perubahan bar & hubungan hanya berlaku pada percakapan PERTAMA. " +
             "Percakapan berikutnya tidak mengubah bar lagi (mencegah farming).")]
    public bool effectsOnlyOnce = true;

    [Tooltip("Node nomor 0 adalah awal dialog")]
    public DialogNode[] nodes;
}

[System.Serializable]
public class DialogNode
{
    [TextArea(2, 6)]
    public string text;

    [Tooltip("Isi 1-3 pilihan. Kosongkan jika node ini hanya kalimat NPC.")]
    public DialogChoice[] choices;

    [Tooltip("Dipakai jika tidak ada pilihan. -1 = dialog selesai")]
    public int nextNode = -1;
}

[System.Serializable]
public class DialogChoice
{
    public string text;

    [Tooltip("Node tujuan setelah memilih. -1 = dialog selesai")]
    public int nextNode = -1;

    [Tooltip("Perubahan bar progres (contoh: 5 atau -10)")]
    public int barChange;

    [Tooltip("Perubahan hubungan dengan NPC ini (contoh: 1 atau -1)")]
    public int relationChange;
}