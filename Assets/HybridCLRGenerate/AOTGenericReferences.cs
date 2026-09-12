using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"AOT.Framework.dll",
		"DOTween.dll",
		"System.Core.dll",
		"UniTask.dll",
		"UnityEngine.CoreModule.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<DistanceFollowing.<Run>d__5>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<HotUpdate.Point.MuseumPointsController.<UnpdataPoint>d__13>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<NMRTKMoveObject.<Run>d__9>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<DistanceFollowing.<Run>d__5>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<HotUpdate.Point.MuseumPointsController.<UnpdataPoint>d__13>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<NMRTKMoveObject.<Run>d__9>
	// Cysharp.Threading.Tasks.ITaskPoolNode<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<Cysharp.Threading.Tasks.AsyncUnit>
	// System.Action<UnityEngine.Color>
	// System.Action<int,object,int>
	// System.Action<object,object>
	// System.Action<object>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.Color>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<UnityEngine.Color>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.ComparisonComparer<UnityEngine.Color>
	// System.Collections.Generic.ComparisonComparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<UnityEngine.Color>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<UnityEngine.Color>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<UnityEngine.Color>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<UnityEngine.Color>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<UnityEngine.Color>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<UnityEngine.Color>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<UnityEngine.Color>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<UnityEngine.Color>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.Color>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<UnityEngine.Color>
	// System.Comparison<object>
	// System.Func<System.Threading.Tasks.VoidTaskResult>
	// System.Func<int>
	// System.Func<object,System.Threading.Tasks.VoidTaskResult>
	// System.Func<object,object>
	// System.Func<object>
	// System.Predicate<UnityEngine.Color>
	// System.Predicate<object>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<object>
	// System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<object>
	// System.Threading.Tasks.Task<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.Task<object>
	// System.Threading.Tasks.TaskFactory<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory<object>
	// }}

	public void RefMethods()
	{
		// System.Threading.Tasks.Task<object> AOT.HotUpdate.Experience.IContentAssetProvider.LoadAsync<object>(string,System.Threading.CancellationToken)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,DistanceFollowing.<Run>d__5>(Cysharp.Threading.Tasks.UniTask.Awaiter&,DistanceFollowing.<Run>d__5&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,HotUpdate.Point.MuseumPointsController.<UnpdataPoint>d__13>(Cysharp.Threading.Tasks.UniTask.Awaiter&,HotUpdate.Point.MuseumPointsController.<UnpdataPoint>d__13&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,NMRTKMoveObject.<Run>d__9>(Cysharp.Threading.Tasks.UniTask.Awaiter&,NMRTKMoveObject.<Run>d__9&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<DistanceFollowing.<Run>d__5>(DistanceFollowing.<Run>d__5&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<HotUpdate.Point.MuseumPointsController.<UnpdataPoint>d__13>(HotUpdate.Point.MuseumPointsController.<UnpdataPoint>d__13&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<NMRTKMoveObject.<Run>d__9>(NMRTKMoveObject.<Run>d__9&)
		// object DG.Tweening.TweenSettingsExtensions.OnComplete<object>(object,DG.Tweening.TweenCallback)
		// object DG.Tweening.TweenSettingsExtensions.SetEase<object>(object,DG.Tweening.Ease)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,HotUpdate.Experience.FeatureRouter.<EnterFeatureAsync>d__6>(System.Runtime.CompilerServices.TaskAwaiter&,HotUpdate.Experience.FeatureRouter.<EnterFeatureAsync>d__6&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,HotUpdate.Experience.FeatureRouter.<ExitCurrentFeatureAsync>d__7>(System.Runtime.CompilerServices.TaskAwaiter&,HotUpdate.Experience.FeatureRouter.<ExitCurrentFeatureAsync>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,HotUpdate.Point.PointBase.<PreloadAsync>d__35>(System.Runtime.CompilerServices.TaskAwaiter<object>&,HotUpdate.Point.PointBase.<PreloadAsync>d__35&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,HotUpdate.Experience.FeatureRouter.<EnterFeatureAsync>d__6>(System.Runtime.CompilerServices.TaskAwaiter&,HotUpdate.Experience.FeatureRouter.<EnterFeatureAsync>d__6&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,HotUpdate.Experience.FeatureRouter.<ExitCurrentFeatureAsync>d__7>(System.Runtime.CompilerServices.TaskAwaiter&,HotUpdate.Experience.FeatureRouter.<ExitCurrentFeatureAsync>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,HotUpdate.Point.PointBase.<PreloadAsync>d__35>(System.Runtime.CompilerServices.TaskAwaiter<object>&,HotUpdate.Point.PointBase.<PreloadAsync>d__35&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<HotUpdate.Experience.FeatureRouter.<EnterFeatureAsync>d__6>(HotUpdate.Experience.FeatureRouter.<EnterFeatureAsync>d__6&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<HotUpdate.Experience.FeatureRouter.<ExitCurrentFeatureAsync>d__7>(HotUpdate.Experience.FeatureRouter.<ExitCurrentFeatureAsync>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<HotUpdate.MuseumHotUpdateEntry.<ShutdownAsync>d__2>(HotUpdate.MuseumHotUpdateEntry.<ShutdownAsync>d__2&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<HotUpdate.Point.PointBase.<PreloadAsync>d__35>(HotUpdate.Point.PointBase.<PreloadAsync>d__35&)
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.Component.GetComponentInChildren<object>()
		// object[] UnityEngine.Component.GetComponentsInChildren<object>()
		// object[] UnityEngine.Component.GetComponentsInChildren<object>(bool)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
		// object UnityEngine.Object.FindObjectOfType<object>()
		// object UnityEngine.Object.Instantiate<object>(object)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform,bool)
	}
}