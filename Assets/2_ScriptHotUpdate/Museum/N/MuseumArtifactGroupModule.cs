using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>管理一个点位下的所有文物组，任何时刻只保留选中项对应的组。</summary>
    [DisallowMultipleComponent]
    public sealed class MuseumArtifactGroupModule : PointModuleBehaviour
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
            CollectGroupsIfNeeded();

            if (hideOnInitialize)
            {
                ClearSelection();
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
                    artifactGroup.SetActive(artifactGroup == selectedGroup);
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

            foreach (ArtifactAutoRotation rotation in
                     selectedGroup.GetComponentsInChildren<ArtifactAutoRotation>(true))
            {
                rotation.StartRotation();
            }
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

            foreach (ArtifactAutoRotation rotation in
                     searchRoot.GetComponentsInChildren<ArtifactAutoRotation>(true))
            {
                rotation.StopRotation();
            }
        }

        public override void ExitPoint(PointExitReason reason)
        {
            ClearSelection();
        }

        public override void Shutdown()
        {
            ClearSelection();
            base.Shutdown();
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
