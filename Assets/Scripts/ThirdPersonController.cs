using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DoorPuzzle
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintSpeed = 6.4f;
        [SerializeField] private float turnSpeed = 14f;

        [Header("Orbit Camera")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.65f, 0.25f, -4.2f);
        [SerializeField] private float lookSensitivity = 0.095f;
        [SerializeField] private float cameraCollisionRadius = 0.22f;
        [SerializeField] private LayerMask cameraCollisionMask = ~0;

        [Header("Targeting")]
        [SerializeField] private float targetRange = 32f;
        [SerializeField] private float targetRadius = 0.32f;
        [SerializeField] private LayerMask targetMask = ~0;

        [Header("Interaction")]
        [SerializeField, Min(0.5f)] private float interactionRange = 3.2f;
        [SerializeField, Min(0.01f)] private float interactionRadius = 0.18f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private CharacterController characterController;
        private ITargetable currentTarget;
        private IInteractable currentInteractable;
        private float yaw;
        private float pitch = 12f;
        private float verticalVelocity;
        private float focusAmount;
        private float focusProgress;
        private bool cursorReleased;
        private bool cinematic;
        private bool targeting;

        public Camera PlayerCamera => playerCamera;
        public bool IsCinematic => cinematic;
        public bool IsTargeting => targeting;
        public ITargetable CurrentTarget => currentTarget;

        public void Initialize(Camera camera)
        {
            playerCamera = camera;
            characterController = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
            EnsureCameraRig();
            LockCursor(true);
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                cursorReleased = !cursorReleased;
                LockCursor(!cursorReleased);
            }

            if (cinematic || cursorReleased || playerCamera == null)
            {
                ClearTarget();
                ClearInteraction();
                return;
            }

            ReadLook();
            ReadMovement();
            UpdateTargeting();
            UpdateInteraction();
        }

        private void LateUpdate()
        {
            if (cinematic || playerCamera == null || cameraPivot == null)
            {
                return;
            }

            cameraPivot.localRotation = Quaternion.Euler(pitch, yaw - transform.eulerAngles.y, 0f);
            var localOffset = shoulderOffset;
            localOffset.z = Mathf.Lerp(shoulderOffset.z, shoulderOffset.z * 0.72f, targeting ? 1f : focusAmount * 0.35f);
            var pivotPosition = cameraPivot.position;
            var desiredPosition = cameraPivot.TransformPoint(localOffset);
            var direction = desiredPosition - pivotPosition;
            var distance = direction.magnitude;
            if (distance > 0.001f && Physics.SphereCast(pivotPosition, cameraCollisionRadius, direction.normalized,
                    out var hit, distance, cameraCollisionMask, QueryTriggerInteraction.Ignore))
            {
                desiredPosition = pivotPosition + direction.normalized * Mathf.Max(0.25f, hit.distance - 0.08f);
            }

            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, desiredPosition,
                1f - Mathf.Exp(-18f * Time.deltaTime));
            var lookPoint = pivotPosition + cameraPivot.forward * 9f;
            playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation,
                Quaternion.LookRotation(lookPoint - playerCamera.transform.position, Vector3.up),
                1f - Mathf.Exp(-20f * Time.deltaTime));
            var targetFov = Mathf.Lerp(62f, 48f, Mathf.Max(targeting ? 0.72f : 0f, Mathf.Max(focusAmount * 0.6f, focusProgress)));
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * 6f);
        }

        private void EnsureCameraRig()
        {
            if (cameraPivot == null)
            {
                var pivotObject = new GameObject("Third Person Camera Pivot");
                cameraPivot = pivotObject.transform;
                cameraPivot.SetParent(transform, false);
                cameraPivot.localPosition = new Vector3(0f, 1.45f, 0f);
            }

            if (playerCamera.transform.parent != null)
            {
                playerCamera.transform.SetParent(null, true);
            }
            playerCamera.transform.position = cameraPivot.TransformPoint(shoulderOffset);
        }

        private void ReadLook()
        {
            var look = Vector2.zero;
            if (Mouse.current != null)
            {
                look += Mouse.current.delta.ReadValue() * lookSensitivity;
            }
            if (Gamepad.current != null)
            {
                look += Gamepad.current.rightStick.ReadValue() * (120f * Time.deltaTime);
            }
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -25f, 68f);
        }

        private void ReadMovement()
        {
            var move = Vector2.zero;
            var sprint = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) move.y += 1f;
                if (Keyboard.current.sKey.isPressed) move.y -= 1f;
                if (Keyboard.current.dKey.isPressed) move.x += 1f;
                if (Keyboard.current.aKey.isPressed) move.x -= 1f;
                sprint = Keyboard.current.leftShiftKey.isPressed;
            }
            if (Gamepad.current != null)
            {
                move += Gamepad.current.leftStick.ReadValue();
                sprint |= Gamepad.current.leftStickButton.isPressed;
            }
            move = Vector2.ClampMagnitude(move, 1f);

            var cameraForward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            var cameraRight = Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up).normalized;
            var planarDirection = Vector3.ClampMagnitude(cameraForward * move.y + cameraRight * move.x, 1f);
            var speed = sprint ? sprintSpeed : walkSpeed;

            if (characterController.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            characterController.Move((planarDirection * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

            var facing = targeting ? cameraForward : planarDirection;
            if (facing.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing, Vector3.up),
                    1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }
        }

        private void UpdateTargeting()
        {
            targeting = (Mouse.current != null && Mouse.current.rightButton.isPressed) ||
                        (Gamepad.current != null && Gamepad.current.leftTrigger.ReadValue() > 0.45f);
            if (!targeting)
            {
                ClearTarget();
                return;
            }

            ITargetable next = null;
            var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            var hits = Physics.SphereCastAll(ray, targetRadius, targetRange, targetMask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                next = hit.collider.GetComponentInParent<ITargetable>();
                if (next != null && next.CanTarget) break;
                next = null;
            }
            SetTarget(next);

            var activate = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                           (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame);
            if (activate && currentTarget != null) currentTarget.Activate(this);
        }

        private void SetTarget(ITargetable next)
        {
            if (ReferenceEquals(next, currentTarget)) return;
            currentTarget?.SetTargeted(false);
            currentTarget = next;
            currentTarget?.SetTargeted(true);
        }

        private void UpdateInteraction()
        {
            IInteractable next = null;
            var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            var playerInteractionOrigin = transform.position + Vector3.up;
            var cameraToPlayerDistance = Vector3.Distance(ray.origin, playerInteractionOrigin);
            var rayDistance = cameraToPlayerDistance + interactionRange;
            var hits = Physics.SphereCastAll(ray, interactionRadius, rayDistance,
                interactionMask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                var closestPoint = hit.collider.ClosestPoint(playerInteractionOrigin);
                if (Vector3.Distance(playerInteractionOrigin, closestPoint) > interactionRange) continue;
                next = hit.collider.GetComponentInParent<IInteractable>();
                if (next != null && next.CanInteract) break;
                next = null;
            }

            SetInteractable(next);
            var interactPressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                                  (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
            if (!interactPressed) return;

            if (currentInteractable != null)
            {
                var component = currentInteractable as Component;
                var objectName = component != null ? component.gameObject.name : "non-Component interactable";
                Debug.Log($"[Interaction] E pressed -> {currentInteractable.GetType().Name} on '{objectName}'.", component);
                currentInteractable.Interact(this);
                return;
            }

            var hitSummary = new System.Text.StringBuilder();
            var displayedHitCount = Mathf.Min(hits.Length, 6);
            for (var index = 0; index < displayedHitCount; index++)
            {
                var hit = hits[index];
                if (index > 0) hitSummary.Append(" | ");
                var playerDistance = Vector3.Distance(playerInteractionOrigin,
                    hit.collider.ClosestPoint(playerInteractionOrigin));
                hitSummary.Append($"{hit.collider.name} layer={hit.collider.gameObject.layer} " +
                                  $"rayDistance={hit.distance:F2} playerDistance={playerDistance:F2}");
            }
            if (hits.Length > displayedHitCount)
                hitSummary.Append($" | +{hits.Length - displayedHitCount} more");

            Debug.LogWarning(
                $"[Interaction] E pressed, but no IInteractable was focused. " +
                $"SphereCast hits={hits.Length}, playerReach={interactionRange:F2}, " +
                $"rayDistance={rayDistance:F2}, radius={interactionRadius:F2}, " +
                $"mask={interactionMask.value}. Hits: {(hits.Length > 0 ? hitSummary.ToString() : "none")}", this);
        }

        private void SetInteractable(IInteractable next)
        {
            if (ReferenceEquals(next, currentInteractable)) return;
            if (currentInteractable != null)
            {
                var previousComponent = currentInteractable as Component;
                Debug.Log($"[Interaction] Focus lost: {currentInteractable.GetType().Name} on " +
                          $"'{(previousComponent != null ? previousComponent.gameObject.name : "unknown")}'.", previousComponent);
            }
            currentInteractable?.SetInteractionFocused(false);
            currentInteractable = next;
            currentInteractable?.SetInteractionFocused(true);
            if (currentInteractable != null)
            {
                var currentComponent = currentInteractable as Component;
                Debug.Log($"[Interaction] Focus acquired: {currentInteractable.GetType().Name} on " +
                          $"'{(currentComponent != null ? currentComponent.gameObject.name : "unknown")}'.", currentComponent);
            }
        }

        private void ClearInteraction()
        {
            SetInteractable(null);
        }

        private void ClearTarget()
        {
            targeting = false;
            SetTarget(null);
        }

        public void SetPuzzleFocus(float alignment, float progress)
        {
            focusAmount = Mathf.Lerp(focusAmount, alignment, Time.deltaTime * 5f);
            focusProgress = Mathf.Lerp(focusProgress, progress, Time.deltaTime * 7f);
        }

        public IEnumerator PlayIntro(Vector3 portalPoint)
        {
            cinematic = true;
            ClearTarget();
            var finalPosition = cameraPivot.TransformPoint(shoulderOffset);
            var startPosition = transform.position + new Vector3(-7.5f, 5.2f, -7f);
            var startRotation = Quaternion.LookRotation(portalPoint + Vector3.up - startPosition, Vector3.up);
            playerCamera.transform.SetParent(null, true);
            playerCamera.transform.position = startPosition;
            playerCamera.transform.rotation = startRotation;
            playerCamera.fieldOfView = 45f;
            const float duration = 3.8f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var p = Mathf.SmoothStep(0f, 1f, t / duration);
                playerCamera.transform.position = Vector3.Lerp(startPosition, finalPosition, p) + Vector3.up * Mathf.Sin(p * Mathf.PI);
                playerCamera.transform.rotation = Quaternion.Slerp(startRotation,
                    Quaternion.LookRotation(cameraPivot.position + cameraPivot.forward * 8f - finalPosition, Vector3.up), p);
                playerCamera.fieldOfView = Mathf.Lerp(45f, 62f, p);
                yield return null;
            }
            cinematic = false;
        }

        public IEnumerator PlaySolveSequence(Vector3 portalPoint)
        {
            cinematic = true;
            ClearTarget();
            var startPosition = playerCamera.transform.position;
            var startRotation = playerCamera.transform.rotation;
            var revealPosition = portalPoint + new Vector3(-2.8f, 2f, -6.2f);
            var revealRotation = Quaternion.LookRotation(portalPoint - revealPosition, Vector3.up);
            const float duration = 1.55f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var p = Mathf.SmoothStep(0f, 1f, t / duration);
                playerCamera.transform.position = Vector3.Lerp(startPosition, revealPosition, p);
                playerCamera.transform.rotation = Quaternion.Slerp(startRotation, revealRotation, p);
                yield return null;
            }
            yield return new WaitForSeconds(0.8f);
            cinematic = false;
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus && !cursorReleased) LockCursor(true);
        }
    }
}
