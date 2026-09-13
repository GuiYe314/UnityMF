using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HotUpdate.Museum.N.Tests
{
    public sealed class PointActivationTests
    {
        [Test]
        public void DistanceGate_UsesExitDistanceAsHysteresis()
        {
            DistanceActivationGate gate = new DistanceActivationGate();

            Assert.That(
                gate.Evaluate(2.9f, 3f, 5f),
                Is.EqualTo(DistanceActivationChange.Entered));
            Assert.That(gate.IsInside, Is.True);

            Assert.That(
                gate.Evaluate(4.9f, 3f, 5f),
                Is.EqualTo(DistanceActivationChange.None));
            Assert.That(gate.IsInside, Is.True);

            Assert.That(
                gate.Evaluate(5f, 3f, 5f),
                Is.EqualTo(DistanceActivationChange.Exited));
            Assert.That(gate.IsInside, Is.False);
        }

        [Test]
        public void DistanceGate_DoesNotEnterOutsideEnterDistance()
        {
            DistanceActivationGate gate = new DistanceActivationGate();

            Assert.That(
                gate.Evaluate(3.1f, 3f, 5f),
                Is.EqualTo(DistanceActivationChange.None));
            Assert.That(gate.IsInside, Is.False);
        }

        [Test]
        public void DistanceGate_RejectsInvalidRange()
        {
            DistanceActivationGate gate = new DistanceActivationGate();

            Assert.Throws<ArgumentException>(
                () => gate.Evaluate(1f, 5f, 3f));
        }

        [Test]
        public void Registry_KeepsPointActiveUntilEverySourceExits()
        {
            object point = new object();
            PointActivationRegistry<object> registry =
                new PointActivationRegistry<object>();
            PointActivationKey distance = new PointActivationKey(
                PointActivationSourceType.Distance,
                1);
            PointActivationKey futureGesture = new PointActivationKey(
                PointActivationSourceType.Gesture,
                2);

            Assert.That(registry.Add(point, distance), Is.True);
            Assert.That(registry.Add(point, futureGesture), Is.True);
            Assert.That(registry.GetSourceCount(point), Is.EqualTo(2));

            Assert.That(registry.Remove(point, distance), Is.True);
            Assert.That(registry.HasAny(point), Is.True);
            Assert.That(registry.GetSourceCount(point), Is.EqualTo(1));

            Assert.That(registry.Remove(point, futureGesture), Is.True);
            Assert.That(registry.HasAny(point), Is.False);
        }

        [Test]
        public void Registry_IgnoresDuplicateSource()
        {
            object point = new object();
            PointActivationRegistry<object> registry =
                new PointActivationRegistry<object>();
            PointActivationKey source = new PointActivationKey(
                PointActivationSourceType.Distance,
                10);

            Assert.That(registry.Add(point, source), Is.True);
            Assert.That(registry.Add(point, source), Is.False);
            Assert.That(registry.GetSourceCount(point), Is.EqualTo(1));
        }

        [Test]
        public void Coordinator_SwitchesBetweenTwoNearbyDistancePoints()
        {
            GameObject root = new GameObject("MuseumPointSystem_ArbitrationTest");

            try
            {
                GameObject cameraObject = new GameObject("Observer");
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
                cameraObject.transform.SetParent(root.transform);
                cameraObject.transform.position = new Vector3(1f, 0f, 0f);

                MuseumPoint pointA =
                    CreatePoint(root.transform, "PointA", Vector3.zero);
                MuseumPoint pointB = CreatePoint(
                    root.transform,
                    "PointB",
                    new Vector3(1f, 0f, 0f));

                MuseumPointCoordinator coordinator =
                    root.AddComponent<MuseumPointCoordinator>();
                typeof(MuseumPointCoordinator)
                    .GetField(
                        "observer",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(coordinator, cameraObject.transform);
                coordinator.Initialize();

                Assert.That(pointA.IsEntered, Is.False);
                Assert.That(pointB.IsEntered, Is.True);

                cameraObject.transform.position = Vector3.zero;
                coordinator.EvaluateNow();

                Assert.That(pointA.IsEntered, Is.True);
                Assert.That(pointB.IsEntered, Is.False);

                coordinator.Shutdown();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static MuseumPoint CreatePoint(
            Transform parent,
            string name,
            Vector3 position)
        {
            GameObject pointObject = new GameObject(name);
            pointObject.transform.SetParent(parent);
            pointObject.transform.position = position;
            MuseumPoint point = pointObject.AddComponent<MuseumPoint>();
            pointObject.AddComponent<DistancePointTrigger>();

            GameObject runtimeObject = new GameObject("Runtime");
            runtimeObject.transform.SetParent(pointObject.transform);
            runtimeObject.AddComponent<PointRuntimeController>();
            return point;
        }
    }
}
