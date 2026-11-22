using System;
using System.Collections;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class MovementAudioReviewV121 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private AirborneFeedbackV121 _presenter;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException("V1.21 movement audio: " + reason); }

        private static void Platform(Transform root, Vector3 position, Vector3 size)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Temporary movement review platform";
            cube.layer = Layers.Environment;
            cube.transform.SetParent(root, false);
            cube.transform.position = position;
            cube.transform.localScale = size;
        }

        [ContextMenu("Movement audio/Run grounded cue checks (resets run)")]
        private void Run() { if (Application.isPlaying && !_running) StartCoroutine(Check()); }

        private IEnumerator Check()
        {
            _running = true;
            GameObject fixture = null;
            try
            {
                Require(_flow != null && _presenter != null && _presenter.OwnsAirborneCues, "Presenter is not bound/enabled.");
                _flow.ReturnToTitle(); _flow.StartNewGame();
                yield return new WaitForSecondsRealtime(.4f);
                _flow.Player.SetControlsEnabled(false);
                CharacterController controller = _flow.Player.Player.GetComponent<CharacterController>();
                Require(controller != null && controller.enabled, "An enabled CharacterController is required.");
                fixture = new GameObject("V1.21 temporary movement fixture");
                Platform(fixture.transform, new Vector3(250f, 82.5f, 250f), new Vector3(6f, 1f, 6f));
                Platform(fixture.transform, new Vector3(250f, 80f, 250f), new Vector3(16f, 1f, 16f));
                float footOffset = controller.bounds.min.y - controller.transform.position.y;
                int jumps = _presenter.JumpRequests, lands = _presenter.LandingRequests;
                _flow.Player.Teleport(new Vector3(250f, 83f - footOffset + .05f, 250f), Quaternion.identity);
                for (int i = 0; i < 5; i++) { controller.Move(Vector3.down * .2f); yield return null; }
                Require(controller.isGrounded, "Fixture did not ground the review capsule.");
                Require(_presenter.JumpRequests == jumps && _presenter.LandingRequests == lands, "Teleport/settling produced an airborne cue.");
                // Coroutines resume before LateUpdate: every Move is sampled by the real presenter.
                float vertical = 8f;
                controller.Move(Vector3.up * vertical * Time.deltaTime); yield return null;
                Require(_presenter.JumpRequests == jumps + 1, "Upward departure did not request exactly one jump.");
                float timeout = Time.time + 3f;
                while (!controller.isGrounded && Time.time < timeout)
                {
                    vertical -= 25f * Time.deltaTime;
                    controller.Move(Vector3.up * vertical * Time.deltaTime); yield return null;
                }
                Require(controller.isGrounded && _presenter.LandingRequests == lands + 1, "Jump landing did not request one landing.");
                Require(_presenter.JumpRequests == jumps + 1, "Airborne samples repeated the jump.");
                jumps = _presenter.JumpRequests; lands = _presenter.LandingRequests;
                controller.Move(Vector3.right * 4f + Vector3.down * .04f); yield return null;
                Require(!controller.isGrounded && _presenter.JumpRequests == jumps, "Walking off a ledge requested a jump.");
                timeout = Time.time + 3f;
                while (!controller.isGrounded && Time.time < timeout) { controller.Move(Vector3.down * 6f * Time.deltaTime); yield return null; }
                Require(controller.isGrounded && _presenter.LandingRequests == lands + 1 && _presenter.JumpRequests == jumps,
                    "Ledge fall did not produce only a landing.");
                lands = _presenter.LandingRequests;
                _flow.Pause(); yield return new WaitForSecondsRealtime(.2f);
                Require(_presenter.JumpRequests == jumps && _presenter.LandingRequests == lands, "Pause replayed motion cues.");
                _flow.Resume(); yield return null;
                _flow.Player.SetControlsEnabled(false);
                Require(_presenter.JumpRequests == jumps && _presenter.LandingRequests == lands, "Resume replayed motion cues.");
                _flow.Retry(); yield return new WaitForSecondsRealtime(.4f);
                Require(_presenter.JumpRequests == jumps && _presenter.LandingRequests == lands, "Retry/settling produced an airborne cue.");
                Debug.Log("V1.21 movement cue requests PASSED. Manually audition jumps, slopes, small steps, ledges and paused airborne motion with the player motor. Counters do not prove audible playback.", this);
            }
            finally
            {
                if (fixture != null) Destroy(fixture);
                if (_flow != null && Application.isPlaying) _flow.ReturnToTitle();
                _running = false;
            }
        }
#endif
    }
}
