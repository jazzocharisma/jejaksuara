using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Tooltip("Teks yang tampil di samping ikon [E], misal: Bicara, Ambil, Buka")]
    public string promptText = "Bicara";

    [Tooltip("Matikan jika objek sementara tidak boleh diajak interaksi")]
    public bool canInteract = true;

    [Tooltip("Tulis pesan ke Console saat berinteraksi (untuk tes)")]
    public bool logToConsole = true;

    [Tooltip("Aksi yang dijalankan saat berinteraksi (atur di Inspector)")]
    public UnityEvent onInteract;

    public void Interact()
    {
        if (!canInteract) return;
        if (logToConsole) Debug.Log("Interaksi dengan: " + gameObject.name);
        onInteract?.Invoke();
    }
}
