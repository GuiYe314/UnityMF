using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>所有点位内消息的公共契约。</summary>
    public interface IPointMessage
    {
        MuseumPoint Point { get; }
    }

    public interface IPointMessageHandler<TMessage>
        where TMessage : struct, IPointMessage
    {
        void Handle(in TMessage message);
    }

    /// <summary>
    /// 点位私有的强类型消息总线。不同点位各自持有实例，避免跨点位串消息。
    /// </summary>
    public sealed class PointMessageBus
    {
        private interface ISubscriptionList
        {
            void Clear();
        }

        private sealed class SubscriptionList<TMessage> : ISubscriptionList
            where TMessage : struct, IPointMessage
        {
            private readonly List<IPointMessageHandler<TMessage>> handlers = new();

            public void Subscribe(IPointMessageHandler<TMessage> handler)
            {
                if (handler == null)
                {
                    return;
                }

                foreach (IPointMessageHandler<TMessage> current in handlers)
                {
                    if (ReferenceEquals(current, handler))
                    {
                        return;
                    }
                }

                handlers.Add(handler);
            }

            public void Unsubscribe(IPointMessageHandler<TMessage> handler)
            {
                for (int i = handlers.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(handlers[i], handler))
                    {
                        handlers.RemoveAt(i);
                    }
                }
            }

            public void Publish(in TMessage message)
            {
                IPointMessageHandler<TMessage>[] snapshot = handlers.ToArray();
                foreach (IPointMessageHandler<TMessage> handler in snapshot)
                {
                    if (handler is UnityEngine.Object unityObject && unityObject == null)
                    {
                        Unsubscribe(handler);
                        continue;
                    }

                    handler.Handle(message);
                }
            }

            public void Clear()
            {
                handlers.Clear();
            }
        }

        private readonly Dictionary<Type, ISubscriptionList> subscriptions = new();

        public void Subscribe<TMessage>(IPointMessageHandler<TMessage> handler)
            where TMessage : struct, IPointMessage
        {
            GetList<TMessage>(true).Subscribe(handler);
        }

        public void Unsubscribe<TMessage>(IPointMessageHandler<TMessage> handler)
            where TMessage : struct, IPointMessage
        {
            GetList<TMessage>(false)?.Unsubscribe(handler);
        }

        public void Publish<TMessage>(in TMessage message)
            where TMessage : struct, IPointMessage
        {
            GetList<TMessage>(false)?.Publish(message);
        }

        public void Clear()
        {
            foreach (ISubscriptionList list in subscriptions.Values)
            {
                list.Clear();
            }

            subscriptions.Clear();
        }

        private SubscriptionList<TMessage> GetList<TMessage>(bool create)
            where TMessage : struct, IPointMessage
        {
            Type messageType = typeof(TMessage);
            if (subscriptions.TryGetValue(
                    messageType,
                    out ISubscriptionList untypedList))
            {
                return (SubscriptionList<TMessage>)untypedList;
            }

            if (!create)
            {
                return null;
            }

            SubscriptionList<TMessage> list = new();
            subscriptions.Add(messageType, list);
            return list;
        }
    }

    /// <summary>需要消息总线的点位模块基类。</summary>
    public abstract class PointMessagingModuleBehaviour : PointModuleBehaviour
    {
        protected PointRuntimeController Runtime { get; private set; }
        protected PointMessageBus Messages { get; private set; }

        public override void Initialize(MuseumPoint point)
        {
            base.Initialize(point);
            Runtime = point != null
                ? point.GetComponentInChildren<PointRuntimeController>(true)
                : null;
            Messages = Runtime?.Messages;

            if (Messages == null)
            {
                Debug.LogError(
                    $"{GetType().Name} 找不到 PointRuntimeController 消息总线。",
                    this);
            }
        }

        public override void Shutdown()
        {
            Messages = null;
            Runtime = null;
            base.Shutdown();
        }
    }

    /// <summary>只处理一种强类型消息的模块基类。</summary>
    public abstract class PointMessageModule<TMessage> :
        PointMessagingModuleBehaviour,
        IPointMessageHandler<TMessage>
        where TMessage : struct, IPointMessage
    {
        public override void Initialize(MuseumPoint point)
        {
            base.Initialize(point);
            Messages?.Subscribe<TMessage>(this);
        }

        public abstract void Handle(in TMessage message);

        public override void Shutdown()
        {
            Messages?.Unsubscribe<TMessage>(this);
            base.Shutdown();
        }
    }

    /// <summary>
    /// 输入适配器基类。输入只发布消息，不引用具体业务模块。
    /// </summary>
    public abstract class PointMessageInputBehaviour : MonoBehaviour
    {
        [SerializeField]
        private PointRuntimeController runtimeController;

        protected PointRuntimeController Runtime
        {
            get
            {
                ResolveRuntime();
                return runtimeController;
            }
        }

        protected bool Publish<TMessage>(in TMessage message)
            where TMessage : struct, IPointMessage
        {
            if (!ResolveRuntime() ||
                runtimeController.Owner == null ||
                !runtimeController.Owner.IsEntered)
            {
                return false;
            }

            runtimeController.Messages.Publish(message);
            return true;
        }

        private bool ResolveRuntime()
        {
            if (runtimeController != null)
            {
                return true;
            }

            runtimeController = GetComponentInParent<PointRuntimeController>(true);
            if (runtimeController != null)
            {
                return true;
            }

            MuseumPoint point = GetComponentInParent<MuseumPoint>(true);
            runtimeController =
                point != null
                    ? point.GetComponentInChildren<PointRuntimeController>(true)
                    : null;
            return runtimeController != null;
        }
    }

    /// <summary>面向具体交互目标的输入适配器基类。</summary>
    public abstract class TargetMessageInputBehaviour :
        PointMessageInputBehaviour
    {
        [SerializeField]
        private MuseumInteractionTarget interactionTarget;

        protected MuseumInteractionTarget Target
        {
            get
            {
                if (interactionTarget == null)
                {
                    interactionTarget =
                        GetComponent<MuseumInteractionTarget>();
                }

                return interactionTarget;
            }
        }

        protected bool PublishInput(
            InteractionInputPhase phase,
            InteractionInputSourceType sourceType)
        {
            if (Target == null || Runtime == null || Runtime.Owner == null)
            {
                return false;
            }

            InteractionInputMessage message = new(
                Runtime.Owner,
                Target,
                phase,
                new InteractionSourceKey(sourceType, GetInstanceID()));
            return Publish(message);
        }
    }
}
