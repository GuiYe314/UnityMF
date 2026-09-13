using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HotUpdate.Museum.N.Tests
{
    public sealed class DistancePointLifecycleTests
    {
        [UnityTest]
        public IEnumerator DistanceTrigger_EntersAndExitsThroughRuntimeController()
        {
            GameObject root = new GameObject("MuseumPointSystem_Test");
            GameObject cameraObject = new GameObject("Observer");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.transform.SetParent(root.transform);
            cameraObject.transform.position = new Vector3(0f, 0f, 10f);

            GameObject pointObject = new GameObject("Point_Test");
            pointObject.transform.SetParent(root.transform);
            pointObject.transform.position = Vector3.zero;
            MuseumPoint point = pointObject.AddComponent<MuseumPoint>();
            pointObject.AddComponent<DistancePointTrigger>();

            GameObject runtimeObject = new GameObject("Runtime");
            runtimeObject.transform.SetParent(pointObject.transform);
            runtimeObject.AddComponent<PointRuntimeController>();
            LifecycleProbeModule probe =
                runtimeObject.AddComponent<LifecycleProbeModule>();

            MuseumPointCoordinator coordinator =
                root.AddComponent<MuseumPointCoordinator>();
            coordinator.Initialize();

            Assert.That(point.IsEntered, Is.False);
            Assert.That(probe.EnterCount, Is.Zero);

            cameraObject.transform.position = new Vector3(0f, 0f, 2f);
            coordinator.EvaluateNow();

            Assert.That(point.IsEntered, Is.True);
            Assert.That(probe.EnterCount, Is.EqualTo(1));

            cameraObject.transform.position = new Vector3(0f, 0f, 4f);
            coordinator.EvaluateNow();
            Assert.That(point.IsEntered, Is.True, "退出距离内应保持激活。");

            cameraObject.transform.position = new Vector3(0f, 0f, 5f);
            coordinator.EvaluateNow();

            Assert.That(point.IsEntered, Is.False);
            Assert.That(probe.ExitCount, Is.EqualTo(1));
            Assert.That(probe.LastExitReason, Is.EqualTo(PointExitReason.NoTrigger));

            Object.Destroy(root);
            yield return null;
        }

        private sealed class LifecycleProbeModule : PointModuleBehaviour
        {
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }
            public PointExitReason LastExitReason { get; private set; }

            public override void EnterPoint()
            {
                EnterCount++;
            }

            public override void ExitPoint(PointExitReason reason)
            {
                ExitCount++;
                LastExitReason = reason;
            }
        }
    }
}
