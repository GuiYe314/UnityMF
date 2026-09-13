using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 单个可选展品的视觉模块。Selected 始终覆盖 Hovered。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MuseumOptionView :
        MonoBehaviour,
        IPointMessageHandler<ModelVisualMessage>,
        IPointMessageHandler<PointLifecycleMessage>
    {
        [SerializeField]
        private Transform scaleTarget;

        [SerializeField]
        private Renderer[] hoverRenderers;

        [Tooltip("可同时包含小模型与对应 BigModel 的 Renderer。")]
        [SerializeField]
        private Renderer[] selectedRenderers;

        [SerializeField]
        private string colorProperty = "_EmissionColor";

        [ColorUsage(true, true)]
        [SerializeField]
        private Color hoverColor = Color.red * 1.5f;

        [ColorUsage(true, true)]
        [SerializeField]
        private Color selectedColor = Color.red * 3f;

        [Min(0.01f)]
        [SerializeField]
        private float selectedScale = 1.5f;

        [SerializeField]
        private UnityEvent onNormal;

        [SerializeField]
        private UnityEvent onHovered;

        [SerializeField]
        private UnityEvent onSelected;

        private readonly Dictionary<Renderer, MaterialPropertyBlock> originalBlocks =
            new();

        private MuseumInteractionTarget target;
        private PointMessageBus messages;
        private Vector3 originalScale;
        private bool hovered;
        private bool selected;
        private bool visualCacheReady;
        private MuseumOptionVisualState visualState;

        public MuseumOptionVisualState VisualState => visualState;
        public bool IsSelected => selected;
        public bool IsHovered => hovered;

        internal void Bind(
            MuseumInteractionTarget owner,
            PointMessageBus messageBus)
        {
            if (target == owner && messages == messageBus)
            {
                return;
            }

            Unbind();
            target = owner;
            messages = messageBus;
            messages?.Subscribe<ModelVisualMessage>(this);
            messages?.Subscribe<PointLifecycleMessage>(this);
            CacheVisualState();
            SetSelected(false);
            SetHovered(false);
        }

        internal void Unbind()
        {
            messages?.Unsubscribe<ModelVisualMessage>(this);
            messages?.Unsubscribe<PointLifecycleMessage>(this);
            RestoreOriginalVisuals();
            messages = null;
            target = null;
            hovered = false;
            selected = false;
        }

        internal void SetHovered(bool value)
        {
            hovered = value;
            RefreshVisualState();
        }

        internal void SetSelected(bool value)
        {
            selected = value;
            RefreshVisualState();
        }

        public void NotifyHoverEnter()
        {
            target?.PublishInput(
                InteractionInputPhase.HoverEnter,
                new InteractionSourceKey(
                    InteractionInputSourceType.Script,
                    GetInstanceID()));
        }

        public void NotifyHoverExit()
        {
            target?.PublishInput(
                InteractionInputPhase.HoverExit,
                new InteractionSourceKey(
                    InteractionInputSourceType.Script,
                    GetInstanceID()));
        }

        public void NotifyClick()
        {
            target?.PublishInput(
                InteractionInputPhase.Click,
                new InteractionSourceKey(
                    InteractionInputSourceType.Script,
                    GetInstanceID()));
        }

        public void Handle(in ModelVisualMessage message)
        {
            if (message.Target != target)
            {
                return;
            }

            selected = message.State == MuseumOptionVisualState.Selected;
            hovered = message.State == MuseumOptionVisualState.Hovered;
            RefreshVisualState();
        }

        public void Handle(in PointLifecycleMessage message)
        {
            if (target == null ||
                message.Point != target.Point ||
                message.Phase != PointLifecyclePhase.Exited)
            {
                return;
            }

            SetSelected(false);
            SetHovered(false);
        }

        private void CacheVisualState()
        {
            if (visualCacheReady)
            {
                return;
            }

            if (scaleTarget == null)
            {
                scaleTarget = transform;
            }

            originalScale = scaleTarget.localScale;
            CacheRendererArray(hoverRenderers);
            CacheRendererArray(selectedRenderers);
            visualCacheReady = true;
        }

        private void CacheRendererArray(Renderer[] renderers)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null || originalBlocks.ContainsKey(targetRenderer))
                {
                    continue;
                }

                MaterialPropertyBlock originalBlock = new();
                targetRenderer.GetPropertyBlock(originalBlock);
                originalBlocks.Add(targetRenderer, originalBlock);
            }
        }

        private void RefreshVisualState()
        {
            CacheVisualState();

            MuseumOptionVisualState nextState = selected
                ? MuseumOptionVisualState.Selected
                : hovered
                    ? MuseumOptionVisualState.Hovered
                    : MuseumOptionVisualState.Normal;

            RestoreOriginalVisuals();

            if (nextState == MuseumOptionVisualState.Selected)
            {
                scaleTarget.localScale = originalScale * selectedScale;
                ApplyColor(selectedRenderers, selectedColor);
            }
            else if (nextState == MuseumOptionVisualState.Hovered)
            {
                ApplyColor(hoverRenderers, hoverColor);
            }

            if (visualState == nextState)
            {
                return;
            }

            visualState = nextState;
            if (visualState == MuseumOptionVisualState.Selected)
            {
                onSelected?.Invoke();
            }
            else if (visualState == MuseumOptionVisualState.Hovered)
            {
                onHovered?.Invoke();
            }
            else
            {
                onNormal?.Invoke();
            }
        }

        private void ApplyColor(Renderer[] renderers, Color color)
        {
            if (renderers == null || string.IsNullOrEmpty(colorProperty))
            {
                return;
            }

            int propertyId = Shader.PropertyToID(colorProperty);
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                MaterialPropertyBlock block = new();
                targetRenderer.GetPropertyBlock(block);
                block.SetColor(propertyId, color);
                targetRenderer.SetPropertyBlock(block);
            }
        }

        private void RestoreOriginalVisuals()
        {
            if (!visualCacheReady)
            {
                return;
            }

            if (scaleTarget != null)
            {
                scaleTarget.localScale = originalScale;
            }

            foreach (KeyValuePair<Renderer, MaterialPropertyBlock> pair in originalBlocks)
            {
                if (pair.Key != null)
                {
                    pair.Key.SetPropertyBlock(pair.Value);
                }
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnValidate()
        {
            selectedScale = Mathf.Max(0.01f, selectedScale);
        }
    }
}
