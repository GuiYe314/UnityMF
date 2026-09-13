using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HotUpdate.Museum.N
{
    public enum MuseumOptionVisualState
    {
        Normal = 0,
        Hovered = 1,
        Selected = 2
    }

    /// <summary>
    /// 单个可选展品的输入入口和视觉状态。Selected 始终覆盖 Hovered。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MuseumOptionView : MonoBehaviour
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

        private MuseumPointSelectionModule selection;
        private int optionIndex = -1;
        private Vector3 originalScale;
        private bool hovered;
        private bool selected;
        private bool visualCacheReady;
        private MuseumOptionVisualState visualState;

        public MuseumOptionVisualState VisualState => visualState;
        public bool IsSelected => selected;
        public bool IsHovered => hovered;

        internal void Bind(MuseumPointSelectionModule owner, int index)
        {
            selection = owner;
            optionIndex = index;
            CacheVisualState();
            SetSelected(false);
            SetHovered(false);
        }

        internal void Unbind()
        {
            RestoreOriginalVisuals();
            selection = null;
            optionIndex = -1;
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
            selection?.SetHovered(optionIndex, true);
        }

        public void NotifyHoverExit()
        {
            selection?.SetHovered(optionIndex, false);
        }

        public void NotifyClick()
        {
            selection?.Select(optionIndex);
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
            RestoreOriginalVisuals();
        }

        private void OnValidate()
        {
            selectedScale = Mathf.Max(0.01f, selectedScale);
        }
    }
}
