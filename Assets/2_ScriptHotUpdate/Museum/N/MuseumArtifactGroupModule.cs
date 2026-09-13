using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>管理一个点位下的所有文物组，任何时刻只保留选中项对应的组。</summary>
    [DisallowMultipleComponent]
    public sealed class MuseumArtifactGroupModule :
        PointMessagingModuleBehaviour,
        IPointMessageHandler<ArtifactMessage>
    {
        [SerializeField]
        private GameObject artifactRoot;

        [Tooltip("为空时自动使用 artifactRoot 的直接子节点。")]
        [SerializeField]
        private List<GameObject> artifactGroups = new();

        [SerializeField]
        private bool hideOnInitialize = true;

        private GameObject selectedGroup;

        public GameObject SelectedGroup => selectedGroup;

        public override void Initialize(MuseumPoint point)
        {
            base.Initialize(point);
            Messages?.Subscribe<ArtifactMessage>(this);
            CollectGroupsIfNeeded();

            if (hideOnInitialize)
            {
                ClearSelection();
            }
        }

        public void Handle(in ArtifactMessage message)
        {
            if (message.Point != Point)
            {
                return;
            }

            switch (message.Command)
            {
                case ArtifactCommand.Prepare:
                    SelectGroup(message.ArtifactGroup);
                    break;
                case ArtifactCommand.Show:
                    if (message.ArtifactGroup != null &&
                        message.ArtifactGroup != selectedGroup)
                    {
                        SelectGroup(message.ArtifactGroup);
                    }

                    ShowSelected();
                    break;
                case ArtifactCommand.Hide:
                    Hide();
                    break;
                case ArtifactCommand.Clear:
                    ClearSelection();
                    break;
            }
        }

        public void SelectGroup(GameObject group)
        {
            StopAllRotation();
            selectedGroup = group;

            foreach (GameObject artifactGroup in artifactGroups)
            {
                if (artifactGroup != null)
                {
                    artifactGroup.SetActive(false);
                }
            }

            if (artifactRoot != null)
            {
                artifactRoot.SetActive(false);
            }
        }

        public void ShowSelected()
        {
            if (selectedGroup == null)
            {
                return;
            }

            selectedGroup.SetActive(true);
            if (artifactRoot != null)
            {
                artifactRoot.SetActive(true);
            }

            PublishRotation(
                ArtifactRotationCommand.Start,
                selectedGroup.transform);
        }

        public void Hide()
        {
            StopAllRotation();

            if (artifactRoot != null)
            {
                artifactRoot.SetActive(false);
            }
            else if (selectedGroup != null)
            {
                selectedGroup.SetActive(false);
            }
        }

        public void ClearSelection()
        {
            StopAllRotation();
            selectedGroup = null;

            foreach (GameObject artifactGroup in artifactGroups)
            {
                artifactGroup?.SetActive(false);
            }

            artifactRoot?.SetActive(false);
        }

        public void StopAllRotation()
        {
            Transform searchRoot =
                artifactRoot != null ? artifactRoot.transform : transform;

            PublishRotation(ArtifactRotationCommand.Stop, searchRoot);
        }

        public override void ExitPoint(PointExitReason reason)
        {
            ClearSelection();
        }

        public override void Shutdown()
        {
            ClearSelection();
            Messages?.Unsubscribe<ArtifactMessage>(this);
            base.Shutdown();
        }

        private void PublishRotation(
            ArtifactRotationCommand command,
            Transform scope)
        {
            if (Messages == null || Point == null || scope == null)
            {
                return;
            }

            ArtifactRotationMessage message =
                new(Point, command, scope);
            Messages.Publish(message);
        }

        private void CollectGroupsIfNeeded()
        {
            if (artifactGroups.Count > 0 || artifactRoot == null)
            {
                return;
            }

            Transform rootTransform = artifactRoot.transform;
            for (int i = 0; i < rootTransform.childCount; i++)
            {
                artifactGroups.Add(rootTransform.GetChild(i).gameObject);
            }
        }
    }
}
