using System;
using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Orthographic orbit camera for the low-poly map. The player rotates, zooms and pans; the camera never
    /// moves on its own and the rotation is not saved. Uses the legacy Input Manager like the rest of the project.
    /// </summary>
    public sealed class MapStyleOrbitCamera : MonoBehaviour
    {
        public Camera Cam;
        public float Yaw = 40f;
        public float Pitch = 36f;
        public float Size = 22f;
        public float MinSize = 5f;
        public float MaxSize = 36f;

        /// <summary>
        /// Distance behind the target. Kept short on purpose: the project's URP shadow distance is 80 and is measured
        /// from the camera, so a far camera would lose all shadows. MaxSize keeps the whole view in front of the camera.
        /// </summary>
        public float Distance = 55f;
        public Vector3 Target;

        /// <summary>Screen position (origin bottom-left) to true when the pointer is over UI and must not drive the camera.</summary>
        public Func<Vector2, bool> IsOverUi;

        private Vector3 targetGoal;
        private float sizeGoal;
        private bool dragging;
        private bool panning;

        public void Place(Vector3 target, float size)
        {
            Target = targetGoal = target;
            Size = sizeGoal = size;
            Apply();
        }

        public void FocusOn(Vector3 target, float size)
        {
            targetGoal = target;
            sizeGoal = size;
        }

        public void Rotate(float degrees)
        {
            Yaw += degrees;
        }

        private void Update()
        {
            if (Cam == null || !Application.isPlaying)
            {
                return;
            }

            var mouse = (Vector2)Input.mousePosition;
            var overUi = IsOverUi != null && IsOverUi(mouse);
            if (Input.GetMouseButtonDown(0)) dragging = !overUi;
            if (Input.GetMouseButtonUp(0)) dragging = false;
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) panning = !overUi;
            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)) panning = false;

            if (dragging)
            {
                Yaw -= Input.GetAxis("Mouse X") * 3.2f;
            }

            var forward = Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
            var right = Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;
            var speed = Size * 1.1f * Time.deltaTime;
            var move = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move -= forward;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += right;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= right;
            if (panning)
            {
                move -= right * Input.GetAxis("Mouse X") * 6f;
                move -= forward * Input.GetAxis("Mouse Y") * 6f;
            }

            targetGoal += move * speed;
            if (Input.GetKey(KeyCode.Q)) Yaw -= 70f * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) Yaw += 70f * Time.deltaTime;
            if (!overUi)
            {
                sizeGoal = Mathf.Clamp(sizeGoal * (1f - Input.mouseScrollDelta.y * 0.08f), MinSize, MaxSize);
            }

            var blend = 1f - Mathf.Exp(-Time.deltaTime * 6f);
            Target = Vector3.Lerp(Target, targetGoal, blend);
            Size = Mathf.Lerp(Size, sizeGoal, blend);
            Apply();
        }

        public void Apply()
        {
            if (Cam == null)
            {
                return;
            }

            Cam.orthographic = true;
            Cam.orthographicSize = Size;
            var rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            Cam.transform.position = Target - rotation * Vector3.forward * Distance;
            Cam.transform.rotation = rotation;
            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = 300f;
        }
    }
}
