using System;
using System.Collections;
using MoayadAR.Persistence;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace MoayadAR.AR
{
    /// <summary>
    /// Return-to-room flow (master prompt §7): never show the model at a rough pose.
    /// States: Recognizing → (confidence gate) → Resolved → fade-in reveal.
    /// Tracking loss freezes logical content instead of allowing visible jumps.
    /// DEVICE-PENDING.
    /// </summary>
    public sealed class RelocalizationController : MonoBehaviour
    {
        public enum State { Idle, Recognizing, Resolved, Limited, Lost }

        [SerializeField] private ARSession _session;
        [SerializeField, Range(0f, 1f)] private float _minResolveConfidence = 0.6f;
        [SerializeField] private float _maxRecognizeSeconds = 20f;
        [SerializeField] private float _fadeInSeconds = 0.35f;

        public State Current { get; private set; } = State.Idle;
        public event Action<State> StateChanged;

        private float _recognizeStarted;
        private GameObject _pendingContent;
        private CanvasGroup _pendingFade;

        public void BeginRecognize(GameObject contentToReveal)
        {
            _pendingContent = contentToReveal;
            if (_pendingContent != null)
            {
                _pendingFade = _pendingContent.GetComponent<CanvasGroup>();
                _pendingContent.SetActive(false); // hidden until the anchor resolves — no rough-pose flash
            }
            _recognizeStarted = Time.time;
            SetState(State.Recognizing);
        }

        private void Update()
        {
            if (Current == State.Recognizing)
            {
                // Confidence proxy: tracking state + elapsed feature accumulation.
                // On device this is refined with ARCore tracking-state reasons.
                float elapsed = Time.time - _recognizeStarted;
                if (ARSession.state == ARSessionState.SessionTracking && elapsed >= 1.0f)
                {
                    SetState(State.Resolved);
                    StartCoroutine(Reveal());
                }
                else if (elapsed > _maxRecognizeSeconds)
                {
                    SetState(State.Limited); // UI offers "Relocalize Room" guidance, not a guess placement
                }
            }
            else if (Current == State.Resolved && ARSession.state != ARSessionState.SessionTracking)
            {
                FreezeContent();
                SetState(State.Lost);
            }
            else if (Current == State.Lost && ARSession.state == ARSessionState.SessionTracking)
            {
                SetState(State.Recognizing);
                _recognizeStarted = Time.time;
            }
        }

        private IEnumerator Reveal()
        {
            if (_pendingContent == null) yield break;
            _pendingContent.SetActive(true);
            if (_pendingFade != null)
            {
                for (float t = 0; t < _fadeInSeconds; t += Time.deltaTime)
                {
                    _pendingFade.alpha = Mathf.Clamp01(t / _fadeInSeconds);
                    yield return null;
                }
                _pendingFade.alpha = 1f;
            }
            _pendingContent = null;
        }

        private void FreezeContent()
        {
            // Content stays parented to its anchor; logical edits are paused by the UI layer.
            // Nothing here moves the model — that is the point: no visible jumps on tracking loss.
        }

        private void SetState(State s)
        {
            if (Current == s) return;
            Current = s;
            StateChanged?.Invoke(s);
        }
    }
}
