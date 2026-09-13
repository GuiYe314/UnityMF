using NUnit.Framework;
using UnityEngine;

namespace HotUpdate.Museum.N.Tests
{
    public sealed class PointMessageArchitectureTests
    {
        private readonly struct TestMessageA : IPointMessage
        {
            public TestMessageA(int value)
            {
                Value = value;
            }

            public MuseumPoint Point => null;
            public int Value { get; }
        }

        private readonly struct TestMessageB : IPointMessage
        {
            public MuseumPoint Point => null;
        }

        private sealed class TestHandler :
            IPointMessageHandler<TestMessageA>,
            IPointMessageHandler<TestMessageB>
        {
            public int ACount { get; private set; }
            public int BCount { get; private set; }
            public int LastValue { get; private set; }

            public void Handle(in TestMessageA message)
            {
                ACount++;
                LastValue = message.Value;
            }

            public void Handle(in TestMessageB message)
            {
                BCount++;
            }
        }

        [Test]
        public void MessageBus_RoutesOnlyMatchingMessageType()
        {
            PointMessageBus bus = new();
            TestHandler handler = new();
            bus.Subscribe<TestMessageA>(handler);

            TestMessageA messageA = new(42);
            TestMessageB messageB = new();
            bus.Publish(messageA);
            bus.Publish(messageB);

            Assert.That(handler.ACount, Is.EqualTo(1));
            Assert.That(handler.LastValue, Is.EqualTo(42));
            Assert.That(handler.BCount, Is.Zero);

            bus.Unsubscribe<TestMessageA>(handler);
            bus.Publish(messageA);
            Assert.That(handler.ACount, Is.EqualTo(1));
        }

        [Test]
        public void ObjectDisplayResolver_TemporaryOverrideRestoresButtonState()
        {
            ObjectDisplayStateResolver resolver = new();
            InteractionSourceKey skillSource = new(
                InteractionInputSourceType.Script,
                10);

            resolver.Reset(ObjectDisplayState.First);
            resolver.Toggle();
            Assert.That(
                resolver.BaseState,
                Is.EqualTo(ObjectDisplayState.Second));

            resolver.SetOverride(
                skillSource,
                ObjectDisplayState.First,
                100);
            Assert.That(
                resolver.ResolvedState,
                Is.EqualTo(ObjectDisplayState.First));

            resolver.Release(skillSource);
            Assert.That(
                resolver.ResolvedState,
                Is.EqualTo(ObjectDisplayState.Second));
        }

        [Test]
        public void ObjectDisplayResolver_HighestPriorityOverrideWins()
        {
            ObjectDisplayStateResolver resolver = new();
            InteractionSourceKey lowPriority = new(
                InteractionInputSourceType.Script,
                1);
            InteractionSourceKey highPriority = new(
                InteractionInputSourceType.Gesture,
                2);

            resolver.Reset(ObjectDisplayState.First);
            resolver.SetOverride(
                lowPriority,
                ObjectDisplayState.Second,
                10);
            resolver.SetOverride(
                highPriority,
                ObjectDisplayState.Hidden,
                20);

            Assert.That(
                resolver.ResolvedState,
                Is.EqualTo(ObjectDisplayState.Hidden));

            resolver.Release(highPriority);
            Assert.That(
                resolver.ResolvedState,
                Is.EqualTo(ObjectDisplayState.Second));
        }

        [Test]
        public void ArtifactRotation_WaitsForEveryHoverSourceToExit()
        {
            GameObject pointObject =
                new GameObject("RotationHoverSourcesTest");

            try
            {
                MuseumPoint point =
                    pointObject.AddComponent<MuseumPoint>();

                GameObject runtimeObject = new GameObject("Runtime");
                runtimeObject.transform.SetParent(pointObject.transform);
                PointRuntimeController runtime =
                    runtimeObject.AddComponent<PointRuntimeController>();

                MuseumInteractionTarget target =
                    CreateTarget(runtimeObject.transform, "Artifact");
                ArtifactAutoRotation rotation =
                    target.gameObject.AddComponent<ArtifactAutoRotation>();

                point.Initialize();
                point.EnterPoint();
                runtime.Messages.Publish(
                    new ArtifactRotationMessage(
                        point,
                        ArtifactRotationCommand.Start,
                        target.transform));

                InteractionSourceKey firstSource = new(
                    InteractionInputSourceType.Mrtk,
                    1);
                InteractionSourceKey secondSource = new(
                    InteractionInputSourceType.Gesture,
                    2);

                target.PublishInput(
                    InteractionInputPhase.HoverEnter,
                    firstSource);
                target.PublishInput(
                    InteractionInputPhase.HoverEnter,
                    secondSource);
                target.PublishInput(
                    InteractionInputPhase.HoverExit,
                    firstSource);

                Assert.That(rotation.IsRunning, Is.True);
                Assert.That(
                    rotation.IsPaused,
                    Is.True,
                    "仍有 Hover 来源时必须保持暂停。");

                target.PublishInput(
                    InteractionInputPhase.HoverExit,
                    secondSource);
                Assert.That(rotation.IsPaused, Is.False);

                point.ExitPoint(PointExitReason.NoTrigger);
                Assert.That(rotation.IsRunning, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(pointObject);
            }
        }

        [Test]
        public void SelectionMessages_KeepSelectedVisualAfterHoverExit()
        {
            GameObject pointObject =
                new GameObject("MessageSelectionTest");

            try
            {
                MuseumPoint point =
                    pointObject.AddComponent<MuseumPoint>();

                GameObject runtimeObject = new GameObject("Runtime");
                runtimeObject.transform.SetParent(pointObject.transform);
                runtimeObject.AddComponent<PointRuntimeController>();
                MuseumPointSelectionModule selection =
                    runtimeObject.AddComponent<MuseumPointSelectionModule>();

                MuseumInteractionTarget targetA =
                    CreateTarget(runtimeObject.transform, "A");
                MuseumInteractionTarget targetB =
                    CreateTarget(runtimeObject.transform, "B");

                point.Initialize();
                point.EnterPoint();

                InteractionSourceKey source = new(
                    InteractionInputSourceType.Script,
                    99);

                targetA.PublishInput(InteractionInputPhase.HoverEnter, source);
                Assert.That(
                    targetA.View.VisualState,
                    Is.EqualTo(MuseumOptionVisualState.Hovered));

                InteractionSourceKey secondSource = new(
                    InteractionInputSourceType.Gesture,
                    100);
                targetA.PublishInput(
                    InteractionInputPhase.HoverEnter,
                    secondSource);
                targetA.PublishInput(
                    InteractionInputPhase.HoverExit,
                    source);
                Assert.That(
                    targetA.View.VisualState,
                    Is.EqualTo(MuseumOptionVisualState.Hovered),
                    "仍有另一个 Hover 来源时不能恢复 Normal。");

                targetA.PublishInput(InteractionInputPhase.Click, source);
                targetA.PublishInput(
                    InteractionInputPhase.HoverExit,
                    secondSource);

                Assert.That(selection.SelectedTarget, Is.SameAs(targetA));
                Assert.That(
                    targetA.View.VisualState,
                    Is.EqualTo(MuseumOptionVisualState.Selected));

                targetB.PublishInput(InteractionInputPhase.Click, source);
                Assert.That(selection.SelectedTarget, Is.SameAs(targetB));
                Assert.That(
                    targetA.View.VisualState,
                    Is.EqualTo(MuseumOptionVisualState.Normal));
                Assert.That(
                    targetB.View.VisualState,
                    Is.EqualTo(MuseumOptionVisualState.Selected));

                point.ExitPoint(PointExitReason.NoTrigger);
                Assert.That(selection.SelectedTarget, Is.Null);
                Assert.That(
                    targetB.View.VisualState,
                    Is.EqualTo(MuseumOptionVisualState.Normal));
            }
            finally
            {
                Object.DestroyImmediate(pointObject);
            }
        }

        private static MuseumInteractionTarget CreateTarget(
            Transform parent,
            string name)
        {
            GameObject targetObject = new GameObject(name);
            targetObject.transform.SetParent(parent);
            MuseumOptionView view =
                targetObject.AddComponent<MuseumOptionView>();
            MuseumInteractionTarget target =
                targetObject.AddComponent<MuseumInteractionTarget>();
            Assert.That(view, Is.Not.Null);
            return target;
        }
    }
}
