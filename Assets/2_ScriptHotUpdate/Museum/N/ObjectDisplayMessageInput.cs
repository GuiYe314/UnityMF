using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 把按钮或 UnityEvent 转换为 ObjectSwitchModule 的强类型消息。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectDisplayMessageInput : PointMessageInputBehaviour
    {
        [SerializeField]
        private string groupId;

        [Tooltip("配置后按钮点击自动发送 Toggle；也可以不配并从 UnityEvent 调用公开方法。")]
        [SerializeField]
        private Button toggleButton;

        [SerializeField]
        private int overridePriority;

        private bool listenerRegistered;

        private InteractionSourceKey Source =>
            new(InteractionInputSourceType.UnityUi, GetInstanceID());

        private void OnEnable()
        {
            if (toggleButton != null && !listenerRegistered)
            {
                toggleButton.onClick.AddListener(Toggle);
                listenerRegistered = true;
            }
        }

        private void OnDisable()
        {
            if (toggleButton != null && listenerRegistered)
            {
                toggleButton.onClick.RemoveListener(Toggle);
                listenerRegistered = false;
            }
        }

        public void Toggle()
        {
            if (!TryGetPoint(out MuseumPoint point))
            {
                return;
            }

            ToggleObjectDisplayMessage message = new(point, groupId);
            Publish(message);
        }

        public void SetBaseFirst()
        {
            SetState(ObjectDisplayState.First, false);
        }

        public void SetBaseSecond()
        {
            SetState(ObjectDisplayState.Second, false);
        }

        public void SetBaseHidden()
        {
            SetState(ObjectDisplayState.Hidden, false);
        }

        public void OverrideFirst()
        {
            SetState(ObjectDisplayState.First, true);
        }

        public void OverrideSecond()
        {
            SetState(ObjectDisplayState.Second, true);
        }

        public void OverrideHidden()
        {
            SetState(ObjectDisplayState.Hidden, true);
        }

        public void ReleaseOverride()
        {
            if (!TryGetPoint(out MuseumPoint point))
            {
                return;
            }

            ReleaseObjectDisplayMessage message =
                new(point, groupId, Source);
            Publish(message);
        }

        private void SetState(ObjectDisplayState state, bool temporary)
        {
            if (!TryGetPoint(out MuseumPoint point))
            {
                return;
            }

            SetObjectDisplayMessage message = new(
                point,
                groupId,
                state,
                Source,
                overridePriority,
                temporary);
            Publish(message);
        }

        private bool TryGetPoint(out MuseumPoint point)
        {
            point = Runtime != null ? Runtime.Owner : null;
            return point != null;
        }

    }
}
