using DG.Tweening;
using HotUpdate.Museum.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace HotUpdate.Museum.Function
{
    /// <summary>
    /// 使用DOTween控制物体缩放。
    ///
    /// 状态优先级：
    /// Select > Hovered > None
    ///
    /// 状态示例：
    /// 1. 移入：添加Hovered，显示悬停缩放。
    /// 2. 点击：添加Select，显示选中缩放。
    /// 3. 移出：移除Hovered；如果仍有Select，继续保持选中缩放。
    /// 4. 取消选择：移除Select；没有其他状态时恢复原始大小。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FunctionObjDotweenScale : FunctionBases
    {
        #region Inspector配置

        [Header("缩放对象")]

        [Tooltip("需要执行缩放的对象。为空时使用当前GameObject的Transform。")]
        [SerializeField]
        private Transform scaleTarget;

        [Tooltip("是否启用缩放功能。关闭后会恢复原始大小，但不会清除交互状态。")]
        [FormerlySerializedAs("zoomEnabled")]
        [SerializeField]
        private bool scaleEnabled = true;

        [Header("状态缩放")]

        [Tooltip("Hovered状态的缩放倍数。")]
        [Min(0f)]
        [SerializeField]
        private float hoveredScaleMultiplier = 1.1f;

        [Tooltip("Select状态的缩放倍数。Select优先级高于Hovered。")]
        [FormerlySerializedAs("zoomSize")]
        [Min(0f)]
        [SerializeField]
        private float selectedScaleMultiplier = 1.5f;

        [Header("动画")]

        [Tooltip("缩放动画持续时间。设置为0时立即完成。")]
        [Min(0f)]
        [SerializeField]
        private float duration = 0.3f;

        [Tooltip("缩放动画缓动类型。")]
        [SerializeField]
        private Ease ease = Ease.OutBack;

        #endregion

        #region 运行时数据

        /// <summary>
        /// 当前物体拥有的全部状态。
        /// 可以同时包含Hovered和Select。
        /// </summary>
        private ObjState currentState = ObjState.None;

        /// <summary>
        /// 初始化时记录的原始本地缩放。
        /// </summary>
        private Vector3 originalLocalScale = Vector3.one;

        /// <summary>
        /// 当前由本组件创建的缩放动画。
        /// 只终止自己的动画，避免影响其他DOTween动画。
        /// </summary>
        private Tween scaleTween;

        /// <summary>
        /// 防止重复初始化覆盖原始缩放。
        /// </summary>
        private bool initialized;

        #endregion

        #region 对外属性

        public ObjState CurrentState => currentState;

        public bool IsHovered => HasState(ObjState.Hovered);

        public bool IsSelected => HasState(ObjState.Select);

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化缩放功能并记录原始大小。
        ///
        /// 必须在实例化后的场景对象上调用，
        /// 不要在AssetBundle加载出来的预制体资源上调用。
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            if (initialized)
            {
                return;
            }

            if (scaleTarget == null)
            {
                scaleTarget = transform;
            }

            originalLocalScale = scaleTarget.localScale;
            currentState = ObjState.None;
            initialized = true;

            // 初始化时立即恢复原始大小。
            RefreshScale(true);
        }

        /// <summary>
        /// 接收Input → FunctionControl传递的缩放事件。
        /// </summary>
        public override void Open(FunctionDataContext functionBasesData)
        {
            base.Open(functionBasesData);

            if (functionBasesData == null)
            {
                Debug.LogWarning(
                    $"{nameof(FunctionObjDotweenScale)}收到空的功能数据。",
                    this);

                return;
            }

            // 即使外部忘记初始化，也保证脚本不会直接报错。
            if (!initialized)
            {
                Initialize();
            }

            switch (functionBasesData.eventName)
            {
                case FunctionEventName.GameObjectDotweenScale_Scale:

                    /*
                     * objStateReversal == false：
                     * 添加对应状态。
                     *
                     * objStateReversal == true：
                     * 移除对应状态。
                     */
                    SetState(
                        functionBasesData.objState,
                        !functionBasesData.objStateReversal);
                    break;

                case FunctionEventName.GameObjectDotweenScale_ReturnScale:

                    /*
                     * 配置了状态时，只移除该状态。
                     * ObjState.None则表示清除全部状态。
                     */
                    if (functionBasesData.objState == ObjState.None)
                    {
                        ClearAllStates();
                    }
                    else
                    {
                        SetState(
                            functionBasesData.objState,
                            false);
                    }

                    break;
            }
        }

        /// <summary>
        /// 对象被关闭时停止动画并恢复原始大小，
        /// 避免下次打开点位时仍然保持放大状态。
        /// </summary>
        private void OnDisable()
        {
            if (!initialized)
            {
                return;
            }

            currentState = ObjState.None;
            StopScaleTween();

            if (scaleTarget != null)
            {
                scaleTarget.localScale = originalLocalScale;
            }
        }

        private void OnDestroy()
        {
            StopScaleTween();

            if (initialized && scaleTarget != null)
            {
                scaleTarget.localScale = originalLocalScale;
            }

            currentState = ObjState.None;
            initialized = false;
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 添加或者移除指定状态。
        ///
        /// 添加Hovered不会覆盖Select；
        /// 移除Hovered也不会移除Select。
        /// </summary>
        public void SetState(ObjState state, bool enabled)
        {
            // 当前功能只处理Hovered和Select。
            state &= ObjState.Hovered | ObjState.Select;

            if (state == ObjState.None)
            {
                return;
            }

            ObjState previousState = currentState;

            if (enabled)
            {
                // 添加状态。
                currentState |= state;
            }
            else
            {
                // 只移除指定状态。
                currentState &= ~state;
            }

            // 状态没有改变时，不重复播放动画。
            if (previousState == currentState)
            {
                return;
            }

            RefreshScale();
        }

        /// <summary>
        /// 判断当前是否包含指定状态。
        /// </summary>
        public bool HasState(ObjState state)
        {
            if (state == ObjState.None)
            {
                return currentState == ObjState.None;
            }

            return (currentState & state) != 0;
        }

        /// <summary>
        /// 清除全部状态并恢复原始大小。
        /// </summary>
        public void ClearAllStates(bool immediate = false)
        {
            currentState = ObjState.None;
            RefreshScale(immediate);
        }

        /// <summary>
        /// 在运行时启用或者关闭缩放功能。
        /// 状态会保留，重新启用后会按照当前状态计算缩放。
        /// </summary>
        public void SetScaleEnabled(bool enabled)
        {
            if (scaleEnabled == enabled)
            {
                return;
            }

            scaleEnabled = enabled;
            RefreshScale();
        }

        #endregion

        #region 缩放优先级

        /// <summary>
        /// 根据当前状态计算最终缩放。
        ///
        /// 优先级：
        /// Select > Hovered > None
        /// </summary>
        private void RefreshScale(bool immediate = false)
        {
            if (!initialized || scaleTarget == null)
            {
                return;
            }

            float multiplier = 1f;

            if (scaleEnabled)
            {
                /*
                 * 必须先判断Select。
                 *
                 * 当状态为Hovered | Select时，
                 * 两个状态都存在，但最终使用Select缩放。
                 */
                if (HasState(ObjState.Select))
                {
                    multiplier = selectedScaleMultiplier;
                }
                else if (HasState(ObjState.Hovered))
                {
                    multiplier = hoveredScaleMultiplier;
                }
            }

            Vector3 targetScale =
                originalLocalScale * multiplier;

            PlayScaleTween(targetScale, immediate);
        }

        /// <summary>
        /// 播放缩放动画。
        /// </summary>
        private void PlayScaleTween(
            Vector3 targetScale,
            bool immediate)
        {
            StopScaleTween();

            if (immediate || duration <= 0f)
            {
                scaleTarget.localScale = targetScale;
                return;
            }

            scaleTween = scaleTarget
                .DOScale(targetScale, duration)
                .SetEase(ease)
                .OnComplete(() => scaleTween = null);
        }

        /// <summary>
        /// 只停止当前组件创建的缩放动画。
        ///
        /// 不使用transform.DOKill()，
        /// 因为它可能同时终止其他脚本创建的动画。
        /// </summary>
        private void StopScaleTween()
        {
            if (scaleTween == null)
            {
                return;
            }

            scaleTween.Kill(false);
            scaleTween = null;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            hoveredScaleMultiplier =
                Mathf.Max(0f, hoveredScaleMultiplier);

            selectedScaleMultiplier =
                Mathf.Max(0f, selectedScaleMultiplier);

            duration = Mathf.Max(0f, duration);
        }
#endif
    }
}