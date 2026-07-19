using System.Collections;
using UnityEngine;

namespace DoorPuzzle
{
    [DisallowMultipleComponent]
    public sealed class VendingPortalLock : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform physicalBarrier;
        [SerializeField] private Renderer signalRenderer;
        [SerializeField] private Light signalLight;
        [SerializeField] private Color lockedColor = new Color(0.42f, 0.08f, 0.035f, 1f);
        [SerializeField] private Color clueColor = new Color(1f, 0.42f, 0.08f, 1f);
        [SerializeField] private Color unlockedColor = new Color(0.2f, 0.72f, 0.58f, 1f);

        private MaterialPropertyBlock propertyBlock;
        private Vector3 barrierClosedPosition;
        private Coroutine feedbackRoutine;
        private bool clueRevealed;
        private bool unlocked;
        private bool focused;

        public bool CanInteract => enabled && gameObject.activeInHierarchy && !unlocked;

        public void Configure(Transform barrier, Renderer indicator, Light indicatorLight)
        {
            physicalBarrier = barrier;
            signalRenderer = indicator;
            signalLight = indicatorLight;
        }

        private void Awake()
        {
            if (physicalBarrier != null)
            {
                barrierClosedPosition = physicalBarrier.localPosition;
                physicalBarrier.gameObject.SetActive(true);
            }
            ApplySignal(lockedColor, 0.35f);
            Debug.Log($"[VendingPortal] Ready on '{name}'. " +
                      $"barrier={(physicalBarrier != null ? physicalBarrier.name : "MISSING")}, " +
                      $"signalRenderer={(signalRenderer != null ? signalRenderer.name : "MISSING")}, " +
                      $"signalLight={(signalLight != null ? signalLight.name : "MISSING")}", this);
        }

        public void RevealClue()
        {
            if (clueRevealed || unlocked)
            {
                Debug.LogWarning($"[VendingPortal] RevealClue ignored. clueRevealed={clueRevealed}, unlocked={unlocked}.", this);
                return;
            }
            clueRevealed = true;
            Debug.Log("[VendingPortal] Clue received. Portal can now be unlocked with E.", this);
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(PulseSignal(clueColor, 3, 0.16f));
        }

        public void SetInteractionFocused(bool value)
        {
            focused = value;
            if (feedbackRoutine == null && !unlocked)
            {
                var color = clueRevealed ? clueColor : lockedColor;
                ApplySignal(color, focused ? 1.8f : 0.55f);
            }
        }

        public void Interact(ThirdPersonController player)
        {
            if (!CanInteract || feedbackRoutine != null)
            {
                Debug.LogWarning($"[VendingPortal] Interaction ignored. CanInteract={CanInteract}, " +
                                 $"sequenceRunning={feedbackRoutine != null}, clueRevealed={clueRevealed}, unlocked={unlocked}.", this);
                return;
            }
            Debug.Log($"[VendingPortal] Interaction accepted. clueRevealed={clueRevealed}. " +
                      $"Action={(clueRevealed ? "unlock door" : "locked pulse")}", this);
            feedbackRoutine = clueRevealed
                ? StartCoroutine(UnlockDoor())
                : StartCoroutine(PulseSignal(lockedColor, 2, 0.1f));
        }

        private IEnumerator PulseSignal(Color color, int count, float duration)
        {
            for (var index = 0; index < count; index++)
            {
                ApplySignal(color, 4f);
                yield return new WaitForSeconds(duration);
                ApplySignal(color, 0.25f);
                yield return new WaitForSeconds(0.13f);
            }
            ApplySignal(color, focused ? 1.8f : 0.55f);
            feedbackRoutine = null;
        }

        private IEnumerator UnlockDoor()
        {
            unlocked = true;
            ApplySignal(unlockedColor, 5f);
            if (physicalBarrier != null)
            {
                var start = physicalBarrier.localPosition;
                var end = barrierClosedPosition + Vector3.up * 5.9f;
                const float duration = 1.65f;
                for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
                {
                    var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    physicalBarrier.localPosition = Vector3.Lerp(start, end, t);
                    yield return null;
                }
                physicalBarrier.gameObject.SetActive(false);
            }
            ApplySignal(unlockedColor, 1.2f);
            Debug.Log("[VendingPortal] Door unlocked. Physical barrier raised and disabled.", this);
            feedbackRoutine = null;
        }

        private void ApplySignal(Color color, float intensity)
        {
            if (signalRenderer != null)
            {
                propertyBlock ??= new MaterialPropertyBlock();
                signalRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_EmissionColor", color * intensity);
                signalRenderer.SetPropertyBlock(propertyBlock);
            }
            if (signalLight != null)
            {
                signalLight.color = color;
                signalLight.intensity = Mathf.Clamp(intensity, 0f, 3.5f);
            }
        }
    }
}
