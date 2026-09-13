using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.Museum.N
{
    /// <summary>把 Unity UI Button 点击转换为视频/文物切换消息。</summary>
    [DisallowMultipleComponent]
    public sealed class MuseumDisplayToggleInput : PointMessageInputBehaviour
    {
        [SerializeField]
        private Button button;

        private bool listenerRegistered;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            RegisterListener();
        }

        private void OnDisable()
        {
            RemoveListener();
        }

        public void SendToggle()
        {
            if (Runtime == null || Runtime.Owner == null)
            {
                return;
            }

            MuseumDisplayToggleMessage message = new(
                Runtime.Owner,
                new InteractionSourceKey(
                    InteractionInputSourceType.UnityUi,
                    GetInstanceID()));
            Publish(message);
        }

        private void RegisterListener()
        {
            if (button != null && !listenerRegistered)
            {
                button.onClick.AddListener(SendToggle);
                listenerRegistered = true;
            }
        }

        private void RemoveListener()
        {
            if (button != null && listenerRegistered)
            {
                button.onClick.RemoveListener(SendToggle);
                listenerRegistered = false;
            }
        }
    }
}
