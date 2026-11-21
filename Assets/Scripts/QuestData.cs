using UnityEngine;

[CreateAssetMenu(fileName = "Quest_", menuName = "Jejak Suara/Quest")]
public class QuestData : ScriptableObject
{
    [Tooltip("ID unik, huruf kecil tanpa spasi (contoh: quest_koordinator)")]
    public string questId = "quest_";

    public string title = "Judul misi";

    [TextArea(2, 4)]
    public string description;

    [Tooltip("Bab tempat misi ini berada (1-4)")]
    [Range(1, 4)]
    public int chapter = 1;

    [Tooltip("Centang = misi wajib (menentukan gate bab). Kosongkan = misi opsional")]
    public bool mandatory = true;

    [Tooltip("Opsional: misi ini baru muncul setelah misi di sini selesai")]
    public QuestData startAfter;

    [Tooltip("Jumlah progres yang dibutuhkan untuk menyelesaikan misi (contoh: 3 teman = 3)")]
    public int requiredCount = 1;

    [Tooltip("Opsional: tiap dialog di daftar ini yang selesai = +1 progres (tiap dialog dihitung sekali)")]
    public DialogData[] progressDialogs;

    [Tooltip("Perubahan bar saat misi selesai (biasanya bonus untuk misi opsional). 0 = tidak ada")]
    public int barReward;
}
