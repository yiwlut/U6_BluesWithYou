using System.Collections;
using UnityEngine;

namespace DoorPuzzle
{
    [DisallowMultipleComponent]
    public sealed class VendingMachinePortalClue : MonoBehaviour, IInteractable
    {
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private AudioSource feedbackAudio;
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
        }

        public void SetInteractionFocused(bool value)
        {
            focused = value;
            if (feedbackRoutine == null && !clueRead)
                ApplyScreenColor(focused ? focusColor : idleColor, focused ? 2.2f : 1f);
        }

        public void Interact(ThirdPersonController player)
        {
            if (!CanInteract || feedbackRoutine != null) return;
            feedbackRoutine = StartCoroutine(PlayClueSequence());
        }

        private IEnumerator PlayClueSequence()
        {
            // Audio feedback is optional. Unity's serialized missing-object sentinel is not
            // reliably handled by C# null-conditional access, so use Unity's null check.
            if (feedbackAudio != null && feedbackAudio.isActiveAndEnabled)
                feedbackAudio.Play();
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
            portalLock?.RevealClue();
            feedbackRoutine = null;
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
