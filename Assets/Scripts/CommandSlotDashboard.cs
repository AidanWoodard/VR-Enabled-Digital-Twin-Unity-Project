using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Dashboard;

public class CommandSlotDashboard : MonoBehaviour
{
    public enum SlotState { Empty, HasRecording, Recording, Playing }

    [Serializable]
    public class SlotRow
    {
        public Image statusDot;
        public Button recordButton;
        public Button playButton;
        public Button stopClearButton;
        public TextMeshProUGUI stopClearLabel;
        public Image clearFillOverlay;  // child Image, fillMethod=Radial360, tinted orange
    }

    [SerializeField] SlotRow[] slots = new SlotRow[5];

    SlotState[] states = new SlotState[5];
    int activeSlot = -1;

    static readonly Color ColorEmpty       = new Color(0.45f, 0.45f, 0.45f);
    static readonly Color ColorHasRecording = new Color(0f,   0.75f, 0.75f);
    static readonly Color ColorRecording   = new Color(0.9f,  0.15f, 0.15f);
    static readonly Color ColorPlaying     = new Color(0.15f, 0.85f, 0.3f);

    const string SVC_RECORD   = "dashboard/record";
    const string SVC_PLAYBACK = "dashboard/playback";
    const string SVC_QUERY    = "dashboard/query_slots";
    const string SVC_CLEAR    = "dashboard/clear";

    readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
    Coroutine clearCoroutine;
    int clearCoroutineSlot = -1;

    void Start()
    {
        var ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<DashboardRecordRequest,     DashboardRecordResponse>(SVC_RECORD);
        ros.RegisterRosService<DashboardPlaybackRequest,   DashboardPlaybackResponse>(SVC_PLAYBACK);
        ros.RegisterRosService<DashboardQuerySlotsRequest, DashboardQuerySlotsResponse>(SVC_QUERY);
        ros.RegisterRosService<DashboardClearRequest,      DashboardClearResponse>(SVC_CLEAR);

        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            slots[i].recordButton.onClick.AddListener(() => OnRecordClicked(idx));
            slots[i].playButton.onClick.AddListener(() => OnPlayClicked(idx));
            WireStopClearButton(idx);
            slots[i].clearFillOverlay.fillAmount = 0f;
            slots[i].clearFillOverlay.gameObject.SetActive(false);
        }

        for (int i = 0; i < 5; i++) UpdateSlotUI(i);

        ros.SendServiceMessage<DashboardQuerySlotsResponse>(
            SVC_QUERY,
            new DashboardQuerySlotsRequest(),
            OnQuerySlotsResponse
        );
    }

    void Update()
    {
        while (mainThreadQueue.TryDequeue(out var action))
            action?.Invoke();
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    void OnQuerySlotsResponse(DashboardQuerySlotsResponse resp)
    {
        mainThreadQueue.Enqueue(() =>
        {
            for (int i = 0; i < 5; i++)
                states[i] = resp.has_recording[i] ? SlotState.HasRecording : SlotState.Empty;
            for (int i = 0; i < 5; i++) UpdateSlotUI(i);
        });
    }

    // ── Record ────────────────────────────────────────────────────────────────

    void OnRecordClicked(int slot)
    {
        if (states[slot] == SlotState.Recording) return;

        if (activeSlot >= 0 && activeSlot != slot)
        {
            StopActiveSlot(() => StartRecording(slot));
            return;
        }
        StartRecording(slot);
    }

    void StartRecording(int slot)
    {
        activeSlot = slot;
        var req = new DashboardRecordRequest(slot + 1, true);
        ROSConnection.GetOrCreateInstance().SendServiceMessage<DashboardRecordResponse>(SVC_RECORD, req, resp =>
        {
            mainThreadQueue.Enqueue(() =>
            {
                if (resp.success)
                {
                    states[slot] = SlotState.Recording;
                    UpdateSlotUI(slot);
                }
                else
                {
                    Debug.LogWarning($"[Dashboard] Record start failed for slot {slot + 1}: {resp.message}");
                    activeSlot = -1;
                }
            });
        });
    }

    // ── Play ──────────────────────────────────────────────────────────────────

    void OnPlayClicked(int slot)
    {
        if (states[slot] != SlotState.HasRecording) return;

        if (activeSlot >= 0 && activeSlot != slot)
        {
            StopActiveSlot(() => StartPlayback(slot));
            return;
        }
        StartPlayback(slot);
    }

    void StartPlayback(int slot)
    {
        activeSlot = slot;
        ROSPublishToggle.IsPublishingEnabled = false;
        var req = new DashboardPlaybackRequest(slot + 1, true);
        ROSConnection.GetOrCreateInstance().SendServiceMessage<DashboardPlaybackResponse>(SVC_PLAYBACK, req, resp =>
        {
            mainThreadQueue.Enqueue(() =>
            {
                if (resp.success)
                {
                    states[slot] = SlotState.Playing;
                    UpdateSlotUI(slot);
                }
                else
                {
                    Debug.LogWarning($"[Dashboard] Playback start failed for slot {slot + 1}: {resp.message}");
                    activeSlot = -1;
                    ROSPublishToggle.IsPublishingEnabled = true;
                }
            });
        });
    }

    // ── Stop (shared for record/play) ─────────────────────────────────────────

    void StopActiveSlot(Action onComplete = null)
    {
        int slot = activeSlot;
        if (slot < 0) { onComplete?.Invoke(); return; }

        if (states[slot] == SlotState.Recording)
        {
            var req = new DashboardRecordRequest(slot + 1, false);
            ROSConnection.GetOrCreateInstance().SendServiceMessage<DashboardRecordResponse>(SVC_RECORD, req, resp =>
            {
                mainThreadQueue.Enqueue(() =>
                {
                    if (resp.success) { states[slot] = SlotState.HasRecording; activeSlot = -1; UpdateSlotUI(slot); }
                    onComplete?.Invoke();
                });
            });
        }
        else if (states[slot] == SlotState.Playing)
        {
            var req = new DashboardPlaybackRequest(slot + 1, false);
            ROSConnection.GetOrCreateInstance().SendServiceMessage<DashboardPlaybackResponse>(SVC_PLAYBACK, req, resp =>
            {
                mainThreadQueue.Enqueue(() =>
                {
                    if (resp.success) { states[slot] = SlotState.HasRecording; activeSlot = -1; ROSPublishToggle.IsPublishingEnabled = true; UpdateSlotUI(slot); }
                    onComplete?.Invoke();
                });
            });
        }
        else
        {
            activeSlot = -1;
            onComplete?.Invoke();
        }
    }

    // ── Stop/Clear button wiring ───────────────────────────────────────────────

    void WireStopClearButton(int slot)
    {
        var go = slots[slot].stopClearButton.gameObject;
        var trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();

        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => OnStopClearDown(slot));
        trigger.triggers.Add(down);

        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => OnStopClearUp(slot));
        trigger.triggers.Add(up);
    }

    void OnStopClearDown(int slot)
    {
        if (states[slot] == SlotState.HasRecording)
        {
            // Begin hold-to-clear
            if (clearCoroutine != null) StopCoroutine(clearCoroutine);
            clearCoroutineSlot = slot;
            slots[slot].clearFillOverlay.gameObject.SetActive(true);
            clearCoroutine = StartCoroutine(ClearHoldRoutine(slot));
        }
        else if (states[slot] == SlotState.Recording || states[slot] == SlotState.Playing)
        {
            StopActiveSlot();
        }
    }

    void OnStopClearUp(int slot)
    {
        // Cancel clear hold if not yet completed
        if (clearCoroutine != null && clearCoroutineSlot == slot)
        {
            StopCoroutine(clearCoroutine);
            clearCoroutine = null;
            clearCoroutineSlot = -1;
            slots[slot].clearFillOverlay.fillAmount = 0f;
            slots[slot].clearFillOverlay.gameObject.SetActive(false);
        }
    }

    IEnumerator ClearHoldRoutine(int slot)
    {
        float elapsed = 0f;
        const float holdTime = 1f;
        var overlay = slots[slot].clearFillOverlay;

        while (elapsed < holdTime)
        {
            elapsed += Time.deltaTime;
            overlay.fillAmount = elapsed / holdTime;
            yield return null;
        }

        overlay.fillAmount = 0f;
        overlay.gameObject.SetActive(false);
        clearCoroutine = null;
        clearCoroutineSlot = -1;

        var req = new DashboardClearRequest(slot + 1);
        ROSConnection.GetOrCreateInstance().SendServiceMessage<DashboardClearResponse>(SVC_CLEAR, req, resp =>
        {
            mainThreadQueue.Enqueue(() =>
            {
                if (resp.success)
                {
                    states[slot] = SlotState.Empty;
                    UpdateSlotUI(slot);
                    Debug.Log($"[Dashboard] Slot {slot + 1} cleared.");
                }
                else
                {
                    Debug.LogWarning($"[Dashboard] Clear failed for slot {slot + 1}: {resp.message}");
                }
            });
        });
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    void UpdateSlotUI(int slot)
    {
        var row = slots[slot];
        var state = states[slot];
        bool otherActive = activeSlot >= 0 && activeSlot != slot;

        row.statusDot.color = state switch
        {
            SlotState.Empty        => ColorEmpty,
            SlotState.HasRecording => ColorHasRecording,
            SlotState.Recording    => ColorRecording,
            SlotState.Playing      => ColorPlaying,
            _                      => ColorEmpty
        };

        row.recordButton.interactable   = state != SlotState.Recording && state != SlotState.Playing && !otherActive;
        row.playButton.interactable     = state == SlotState.HasRecording && !otherActive;

        bool showStopClear = state != SlotState.Empty;
        row.stopClearButton.gameObject.SetActive(showStopClear);
        if (showStopClear)
        {
            bool isStopMode = state == SlotState.Recording || state == SlotState.Playing;
            row.stopClearLabel.text = isStopMode ? "STOP" : "CLEAR";
            row.stopClearButton.interactable = true;
        }
    }
}
