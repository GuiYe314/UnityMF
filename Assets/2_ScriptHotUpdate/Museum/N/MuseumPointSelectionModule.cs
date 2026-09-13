using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace HotUpdate.Museum.N
{
    public enum MuseumPointDisplayMode
    {
        Video = 0,
        Artifact = 1
    }

    [Serializable]
    public sealed class MuseumOption
    {
        [SerializeField]
        private string optionId;

        [SerializeField]
        private MuseumOptionView view;

        [SerializeField]
        private VideoClip videoClip;

        [SerializeField]
        private GameObject artifactGroup;

        [Tooltip("只配置这个展品独有的功能；可为空。")]
        [SerializeField]
        private List<MuseumOptionAction> actions = new();

        public string OptionId => optionId;
        public MuseumOptionView View => view;
        public VideoClip VideoClip => videoClip;
        public GameObject ArtifactGroup => artifactGroup;
        public IReadOnlyList<MuseumOptionAction> Actions => actions;
    }

    /// <summary>
    /// 管理一个展品合集。每个 MuseumOption 集中保存点击对象、视频、文物组和扩展技能。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MuseumPointSelectionModule : PointModuleBehaviour
    {
        [SerializeField]
        private List<MuseumOption> options = new();

        [SerializeField]
        private PointVideoModule videoModule;

        [SerializeField]
        private MuseumArtifactGroupModule artifactModule;

        [Tooltip("点击展品后显示的按钮区域。")]
        [SerializeField]
        private GameObject buttonRoot;

        [Tooltip("切换视频/文物的按钮；为空表示没有切换按钮。")]
        [SerializeField]
        private Button displayToggleButton;

        [SerializeField]
        private bool selectFirstOnEnter;

        private int selectedIndex = -1;
        private bool listenerRegistered;
        private bool pointActive;

        public int SelectedIndex => selectedIndex;
        public MuseumPointDisplayMode DisplayMode { get; private set; }

        public override void Initialize(MuseumPoint point)
        {
            base.Initialize(point);

            if (videoModule == null)
            {
                videoModule = GetComponentInChildren<PointVideoModule>(true);
            }

            if (artifactModule == null)
            {
                artifactModule =
                    GetComponentInChildren<MuseumArtifactGroupModule>(true);
            }

            for (int i = 0; i < options.Count; i++)
            {
                MuseumOption option = options[i];
                option?.View?.Bind(this, i);

                if (option == null)
                {
                    continue;
                }

                MuseumOptionContext context = CreateContext(option, i);
                foreach (MuseumOptionAction action in option.Actions)
                {
                    action?.Initialize(context);
                }
            }

            if (displayToggleButton != null && !listenerRegistered)
            {
                displayToggleButton.onClick.AddListener(ToggleDisplayMode);
                listenerRegistered = true;
            }

            ResetPointContent();
        }

        public override void EnterPoint()
        {
            pointActive = true;
            ResetPointContent();

            if (selectFirstOnEnter && options.Count > 0)
            {
                Select(0);
            }
        }

        public void SetHovered(int optionIndex, bool hovered)
        {
            if (!TryGetOption(optionIndex, out MuseumOption option))
            {
                return;
            }

            option.View?.SetHovered(hovered);
        }

        public void Select(int optionIndex)
        {
            if (Point == null ||
                !Point.IsEntered ||
                !TryGetOption(optionIndex, out MuseumOption option))
            {
                return;
            }

            if (selectedIndex == optionIndex)
            {
                return;
            }

            DeselectCurrent();
            selectedIndex = optionIndex;
            DisplayMode = MuseumPointDisplayMode.Video;

            option.View?.SetSelected(true);
            artifactModule?.SelectGroup(option.ArtifactGroup);

            if (buttonRoot != null)
            {
                buttonRoot.SetActive(true);
            }

            videoModule?.Play(option.VideoClip);

            MuseumOptionContext context = CreateContext(option, optionIndex);
            foreach (MuseumOptionAction action in option.Actions)
            {
                action?.OnSelected(context);
            }
        }

        public void ToggleDisplayMode()
        {
            if (!TryGetOption(selectedIndex, out MuseumOption option))
            {
                return;
            }

            if (DisplayMode == MuseumPointDisplayMode.Video)
            {
                videoModule?.StopAndHide();
                artifactModule?.ShowSelected();
                DisplayMode = MuseumPointDisplayMode.Artifact;
            }
            else
            {
                artifactModule?.Hide();
                videoModule?.Play(option.VideoClip);
                DisplayMode = MuseumPointDisplayMode.Video;
            }
        }

        public void ClearSelection()
        {
            DeselectCurrent();
            ResetPointContent();
        }

        public override void ExitPoint(PointExitReason reason)
        {
            if (!pointActive)
            {
                ResetPointContent();
                return;
            }

            pointActive = false;
            DeselectCurrent();

            for (int i = 0; i < options.Count; i++)
            {
                MuseumOption option = options[i];
                if (option == null)
                {
                    continue;
                }

                MuseumOptionContext context = CreateContext(option, i);
                foreach (MuseumOptionAction action in option.Actions)
                {
                    action?.OnPointExit(context, reason);
                }
            }

            ResetPointContent();
        }

        public override void Shutdown()
        {
            if (pointActive)
            {
                ExitPoint(PointExitReason.Shutdown);
            }
            else
            {
                ResetPointContent();
            }

            if (displayToggleButton != null && listenerRegistered)
            {
                displayToggleButton.onClick.RemoveListener(ToggleDisplayMode);
                listenerRegistered = false;
            }

            for (int i = 0; i < options.Count; i++)
            {
                MuseumOption option = options[i];
                if (option == null)
                {
                    continue;
                }

                MuseumOptionContext context = CreateContext(option, i);
                foreach (MuseumOptionAction action in option.Actions)
                {
                    action?.Shutdown(context);
                }

                option.View?.Unbind();
            }

            base.Shutdown();
        }

        private void DeselectCurrent()
        {
            if (!TryGetOption(selectedIndex, out MuseumOption option))
            {
                selectedIndex = -1;
                return;
            }

            int oldIndex = selectedIndex;
            selectedIndex = -1;
            option.View?.SetSelected(false);

            MuseumOptionContext context = CreateContext(option, oldIndex);
            foreach (MuseumOptionAction action in option.Actions)
            {
                action?.OnDeselected(context);
            }
        }

        private void ResetPointContent()
        {
            selectedIndex = -1;
            DisplayMode = MuseumPointDisplayMode.Video;

            foreach (MuseumOption option in options)
            {
                option?.View?.SetSelected(false);
                option?.View?.SetHovered(false);
            }

            videoModule?.StopAndHide();
            artifactModule?.ClearSelection();

            if (buttonRoot != null)
            {
                buttonRoot.SetActive(false);
            }
        }

        private bool TryGetOption(int index, out MuseumOption option)
        {
            option = null;
            if (index < 0 || index >= options.Count)
            {
                return false;
            }

            option = options[index];
            return option != null;
        }

        private MuseumOptionContext CreateContext(MuseumOption option, int index)
        {
            return new MuseumOptionContext(Point, this, option, index);
        }
    }
}
