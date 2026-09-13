using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 文物旋转组件。移入只暂停当前文物，退出点位时由文物组模块统一停止。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArtifactAutoRotation :
        MonoBehaviour,
        IMixedRealityFocusHandler
    {
        [SerializeField]
        private Vector3 degreesPerSecond = new(0f, 30f, 0f);

        [SerializeField]
        private Space rotationSpace = Space.Self;

        private bool running;
        private bool hovered;

        public bool IsRunning => running;
        public bool IsPaused => hovered;

        private void Update()
        {
            if (!running || hovered)
            {
                return;
            }

            transform.Rotate(
                degreesPerSecond * Time.deltaTime,
                rotationSpace);
        }

        public void StartRotation()
        {
            running = true;
        }

        public void StopRotation()
        {
            running = false;
            hovered = false;
        }

        /// <summary>供 MRTK/其他输入适配器在移入时调用。</summary>
        public void NotifyHoverEnter()
        {
            hovered = true;
        }

        /// <summary>供 MRTK/其他输入适配器在移出时调用。</summary>
        public void NotifyHoverExit()
        {
            hovered = false;
        }

        public void OnFocusEnter(FocusEventData eventData)
        {
            NotifyHoverEnter();
        }

        public void OnFocusExit(FocusEventData eventData)
        {
            NotifyHoverExit();
        }

        private void OnDisable()
        {
            StopRotation();
        }
    }
}
