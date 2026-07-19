using System.Collections;
using UnityEngine;

namespace DoorPuzzle
{
    [DisallowMultipleComponent]
    public sealed class VendingMachinePortalClue : MonoBehaviour, IInteractable
    {
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private AudioSource feedbackAudio;
        [SerializeField] private AudioClip feedbackClip;
        [SerializeField] private VendingPortalLock portalLock;
        [SerializeField] private Color idleColor = new Color(0.12f, 0.28f, 0.34f, 1f);
        [SerializeField] private Color focusColor = new Color(0.22f, 0.7f, 0.82f, 1f);
        [SerializeField] private Color clueColor = new Color(1f, 0.48f, 0.12f, 1f);

        private MaterialPropertyBlock propertyBlock;
        private Coroutine feedbackRoutine;
        private bool focused;
        private bool clueRead;

        public bool CanInteract => enabled && gameObject.activeInHierarchy && !clueRead;

        private void Awake()
        {
            ResolveReferences();
            ApplyScreenColor(idleColor, 1f);
            Debug.Log($"[VendingClue] Ready on '{name}'. " +
                      $"screenRenderer={(screenRenderer != null ? screenRenderer.name : "MISSING")}, " +
                      $"portalLock={(portalLock != null ? portalLock.name : "MISSING")}, " +
                      $"feedbackAudio={(feedbackAudio != null ? feedbackAudio.name : "optional/missing")}, " +
                      $"feedbackClip={(feedbackClip != null ? feedbackClip.name : "MISSING")}", this);
        }

        public void SetInteractionFocused(bool value)
        {
            focused = value;
            if (feedbackRoutine == null && !clueRead)
                ApplyScreenColor(focused ? focusColor : idleColor, focused ? 2.2f : 1f);
        }

        public void Interact(ThirdPersonController player)
        {
            if (!CanInteract || feedbackRoutine != null)
            {
                Debug.LogWarning($"[VendingClue] Interaction ignored. CanInteract={CanInteract}, " +
                                 $"sequenceRunning={feedbackRoutine != null}, clueRead={clueRead}.", this);
                return;
            }
            Debug.Log("[VendingClue] Interaction accepted. Starting clue pulse sequence.", this);
            PlayFeedbackSound();
            feedbackRoutine = StartCoroutine(PlayClueSequence());
        }

        private IEnumerator PlayClueSequence()
        {
            var pulseDurations = new[] { 0.16f, 0.16f, 0.48f };
            foreach (var duration in pulseDurations)
            {
                ApplyScreenColor(clueColor, 5f);
                yield return new WaitForSeconds(duration);
                ApplyScreenColor(idleColor, 0.45f);
                yield return new WaitForSeconds(0.14f);
            }

            clueRead = true;
            ApplyScreenColor(clueColor, 1.7f);
            if (portalLock != null)
            {
                portalLock.RevealClue();
                Debug.Log("[VendingClue] Clue completed and portal lock notified.", this);
            }
            else
            {
                Debug.LogError("[VendingClue] Clue completed, but no VendingPortalLock was found.", this);
            }
            feedbackRoutine = null;
        }

        private void PlayFeedbackSound()
        {
            if (feedbackClip != null)
            {
                if (feedbackAudio != null && feedbackAudio.isActiveAndEnabled)
                    feedbackAudio.PlayOneShot(feedbackClip);
                else
                    AudioSource.PlayClipAtPoint(feedbackClip, transform.position, 1f);
                return;
            }

            if (feedbackAudio != null && feedbackAudio.isActiveAndEnabled)
                feedbackAudio.Play();
            else
                Debug.LogWarning("[VendingClue] Interaction sound could not play because no AudioClip is assigned.", this);
        }

        private void ResolveReferences()
        {
            if (screenRenderer == null)
            {
                foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                {
                    var material = renderer.sharedMaterial;
                    if (renderer.name.IndexOf("screen", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (material != null && material.name.IndexOf("VendingMachineScreen",
                            System.StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        screenRenderer = renderer;
                        break;
                    }
                }
            }
            if (portalLock == null)
                portalLock = FindFirstObjectByType<VendingPortalLock>(FindObjectsInactive.Include);
            propertyBlock ??= new MaterialPropertyBlock();
        }

        private void ApplyScreenColor(Color color, float intensity)
        {
            if (screenRenderer == null) return;
            propertyBlock ??= new MaterialPropertyBlock();
            screenRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_EmissionColor", color * intensity);
            screenRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
