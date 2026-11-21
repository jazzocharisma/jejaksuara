using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    // Sistem lain (bar progres, hubungan NPC, misi) cukup berlangganan event ini
    public static event Action<int> OnBarChange;
    public static event Action<string, int> OnRelationChange;
    public static event Action<DialogData> OnDialogEnded;

    [Header("UI")]
    public GameObject panel;
    public TMP_Text nameText;
    public TMP_Text bodyText;
    public GameObject continueHint;
    public Button[] choiceButtons;

    [Header("Efek Ketik")]
    public float charsPerSecond = 45f;

    [Header("Kamera Dialog")]
    public CinemachineCamera dialogCam;
    public float camDistance = 1.8f;
    public float camSideOffset = 1.0f;
    public float camHeightOffset = 0.15f;

    [Header("Referensi Pemain")]
    public PlayerController player;
    public InteractionSystem interaction;
    public ThirdPersonCameraRig cameraRig;

    [Header("Suara 'blip' (opsional)")]
    public AudioSource blipSource;
    public AudioClip blipClip;

    DialogData data;
    DialogNPC npc;
    DialogNode node;
    bool typing;
    bool skipRequested;
    bool waitingChoice;
    int startFrame;
    Coroutine typeRoutine;

    // Dialog yang sudah pernah selesai (untuk efek sekali saja & dialog ulang)
    readonly HashSet<DialogData> finishedDialogs = new HashSet<DialogData>();

    public bool HasCompleted(DialogData d)
    {
        return d != null && finishedDialogs.Contains(d);
    }

    void Awake()
    {
        Instance = this;
        IsOpen = false;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            choiceButtons[i].onClick.AddListener(() => SelectChoice(index));
        }

        if (panel != null) panel.SetActive(false);
        if (dialogCam != null) dialogCam.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        IsOpen = false;
    }

    public void StartDialog(DialogData d, DialogNPC n)
    {
        if (IsOpen || d == null || d.nodes == null || d.nodes.Length == 0) return;

        data = d;
        npc = n;
        IsOpen = true;
        startFrame = Time.frameCount;

        // Kunci kontrol pemain
        if (player != null) player.inputEnabled = false;
        if (interaction != null) interaction.interactionEnabled = false;
        if (cameraRig != null) cameraRig.SetLookEnabled(false);

        if (nameText != null) nameText.text = d.npcName;

        FaceEachOther();
        SetupDialogCamera();

        panel.SetActive(true);
        ShowNode(0);
    }

    void FaceEachOther()
    {
        if (player == null || npc == null) return;

        Vector3 toPlayer = player.transform.position - npc.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) return;

        npc.transform.rotation = Quaternion.LookRotation(toPlayer);
        player.transform.rotation = Quaternion.LookRotation(-toPlayer);
    }

    void SetupDialogCamera()
    {
        if (dialogCam == null || npc == null || player == null) return;

        Vector3 face = npc.FacePoint;
        Vector3 toPlayer = player.transform.position - face;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = -npc.transform.forward;
        toPlayer.Normalize();

        // Sedikit ke samping supaya badan pemain tidak menutupi wajah NPC
        Vector3 side = Vector3.Cross(Vector3.up, toPlayer);
        Vector3 camPos = face + toPlayer * camDistance + side * camSideOffset + Vector3.up * camHeightOffset;

        dialogCam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(face - camPos));
        dialogCam.gameObject.SetActive(true);
    }

    void ShowNode(int index)
    {
        if (index < 0 || index >= data.nodes.Length)
        {
            EndDialog();
            return;
        }

        node = data.nodes[index];
        waitingChoice = false;
        HideChoices();
        if (continueHint != null) continueHint.SetActive(false);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeText(node.text));
    }

    IEnumerator TypeText(string text)
    {
        typing = true;
        skipRequested = false;

        bodyText.text = text;
        bodyText.maxVisibleCharacters = 0;

        int total = text.Length;
        float shown = 0f;
        int lastShown = 0;

        while (lastShown < total && !skipRequested)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            int target = Mathf.Min(total, Mathf.FloorToInt(shown));

            if (target > lastShown)
            {
                if (blipSource != null && blipClip != null
                    && !char.IsWhiteSpace(text[target - 1]) && target % 2 == 0)
                {
                    blipSource.PlayOneShot(blipClip);
                }
                lastShown = target;
                bodyText.maxVisibleCharacters = lastShown;
            }
            yield return null;
        }

        bodyText.maxVisibleCharacters = int.MaxValue;
        typing = false;

        if (node.choices != null && node.choices.Length > 0) ShowChoices();
        else if (continueHint != null) continueHint.SetActive(true);
    }

    void ShowChoices()
    {
        waitingChoice = true;
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool active = i < node.choices.Length;
            choiceButtons[i].gameObject.SetActive(active);
            if (!active) continue;

            TMP_Text label = choiceButtons[i].GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = (i + 1) + ". " + node.choices[i].text;
        }
    }

    void HideChoices()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
            choiceButtons[i].gameObject.SetActive(false);
    }

    void Update()
    {
        // Abaikan input di frame yang sama saat dialog dibuka (tombol E yang membukanya)
        if (!IsOpen || Time.frameCount == startFrame) return;

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (waitingChoice)
        {
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) SelectChoice(0);
                else if (kb.digit2Key.wasPressedThisFrame) SelectChoice(1);
                else if (kb.digit3Key.wasPressedThisFrame) SelectChoice(2);
            }
            return;
        }

        bool advance = (kb != null && kb.eKey.wasPressedThisFrame)
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (!advance) return;

        if (typing)
        {
            skipRequested = true; // tampilkan seluruh teks sekarang
            return;
        }

        ShowNode(node.nextNode);
    }

    void SelectChoice(int i)
    {
        if (!IsOpen || !waitingChoice || node.choices == null || i < 0 || i >= node.choices.Length) return;

        DialogChoice c = node.choices[i];
        waitingChoice = false;
        HideChoices();

        // Efek hanya berlaku di percakapan pertama jika effectsOnlyOnce dicentang
        bool applyEffects = !(data.effectsOnlyOnce && finishedDialogs.Contains(data));

        if (!applyEffects)
        {
            Debug.Log("[Dialog] Dialog sudah pernah selesai, efek bar/hubungan diabaikan");
        }
        else
        {
            if (c.barChange != 0)
            {
                Debug.Log("[Dialog] Bar progres: " + (c.barChange > 0 ? "+" : "") + c.barChange);
                OnBarChange?.Invoke(c.barChange);
            }
            if (c.relationChange != 0 && npc != null)
            {
                Debug.Log("[Dialog] Hubungan " + npc.npcId + ": " + (c.relationChange > 0 ? "+" : "") + c.relationChange);
                OnRelationChange?.Invoke(npc.npcId, c.relationChange);
            }
        }

        ShowNode(c.nextNode);
    }

    void EndDialog()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typing = false;
        waitingChoice = false;
        IsOpen = false;

        // Catat bahwa dialog ini sudah pernah selesai
        if (data != null) finishedDialogs.Add(data);

        panel.SetActive(false);
        if (dialogCam != null) dialogCam.gameObject.SetActive(false);

        StartCoroutine(ReleaseControl(data));
    }

    IEnumerator ReleaseControl(DialogData finished)
    {
        // Tunggu 1 frame supaya tombol penutup dialog tidak langsung memicu interaksi lagi
        yield return null;

        if (player != null) player.inputEnabled = true;
        if (interaction != null) interaction.interactionEnabled = true;
        if (cameraRig != null) cameraRig.SetLookEnabled(true);

        OnDialogEnded?.Invoke(finished);
    }
}