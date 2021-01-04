#pragma warning disable 649 // Field `Drawing.GizmoContext.activeTransform' is never assigned to, and will always have its default value `null'. Not used outside of the unity editor.
using UnityEngine;
using System.Collections;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Drawing {
	public static class GizmoContext {
		static Transform activeTransform;
		static HashSet<Transform> selectedTransforms = new HashSet<Transform>();

		static internal bool drawingGizmos;

		/// <summary>Number of top-level transforms that are selected</summary>
		public static int selectionSize { get; private set; }

		internal static void Refresh () {
#if UNITY_EDITOR
			activeTransform = Selection.activeTransform;
			selectedTransforms.Clear();
			var topLevel = Selection.transforms;
			for (int i = 0; i < topLevel.Length; i++) selectedTransforms.Add(topLevel[i]);
			selectionSize = topLevel.Length;
#endif
		}

		/// <summary>
		/// True if the component is selected.
		/// This is a deep selection: even children of selected transforms are considered to be selected.
		/// </summary>
		public static bool InSelection (Component c) {
			if (!drawingGizmos) throw new System.Exception("Can only be used inside the Drawing library's gizmo drawing functions.");
			return InSelection(c.transform);
		}

		/// <summary>
		/// True if the transform is selected.
		/// This is a deep selection: even children of selected transforms are considered to be selected.
		/// </summary>
		public static bool InSelection (Transform tr) {
			if (!drawingGizmos) throw new System.Exception("Can only be used inside the Drawing library's gizmo drawing functions.");
			var leaf = tr;
			while (tr != null) {
				if (selectedTransforms.Contains(tr)) {
					selectedTransforms.Add(leaf);
					return true;
				}
				tr = tr.parent;
			}
			return false;
		}

		/// <summary>
		/// True if the component is shown in the inspector.
		/// The active selection is the GameObject that is currently visible in the inspector.
		/// </summary>
		public static bool InActiveSelection (Component c) {
			if (!drawingGizmos) throw new System.Exception("Can only be used inside the Drawing library's gizmo drawing functions.");
			return InActiveSelection(c.transform);
		}

		/// <summary>
		/// True if the transform is shown in the inspector.
		/// The active selection is the GameObject that is currently visible in the inspector.
		/// </summary>
		public static bool InActiveSelection (Transform tr) {
			if (!drawingGizmos) throw new System.Exception("Can only be used inside the Drawing library's gizmo drawing functions.");
			return tr.transform == activeTransform;
		}
	}

	public interface IDrawGizmosWithVersion : IDrawGizmos {
		int GizmoVersion { get; }
	}

	public interface IDrawGizmos {
		void DrawGizmos ();
	}

	[ExecuteAlways]
	[AddComponentMenu("")]
	[HelpURL("http://arongranberg.com/astar/docs/class_drawing_1_1_retained_gizmos_wrapper.php")]
	public class RetainedGizmosWrapper : MonoBehaviour {
		public RetainedGizmos gizmos = new RetainedGizmos();
		static List<IDrawGizmos> gizmoDrawers = new List<IDrawGizmos>();
		static RetainedGizmosWrapper _instance;
		bool framePassed;
		int lastFrameCount = int.MinValue;
		bool builtGizmos;
		RetainedGizmos.RedrawScope previousFrameRedrawScope;
		RetainedGizmos.RedrawScope frameRedrawScope;
		public static bool allowRenderToRenderTextures = false;
		public static bool drawToAllCameras = false;
		public static int gizmoVersion = 0;
		CommandBuffer commandBuffer;

		public static RetainedGizmosWrapper instance {
			get {
				if (_instance == null) Init();
				return _instance;
			}
		}

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
#endif
		public static void Init () {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if (Unity.Jobs.LowLevel.Unsafe.JobsUtility.IsExecutingJob) throw new System.Exception("Draw.* methods cannot be called from inside a job. See the documentation for info about how to use drawing functions from the Unity Job System.");
#endif
			if (_instance != null) return;

			// Find any existing instance.
			// Includes objects on HideInInspector GameObjects
			var instances = Resources.FindObjectsOfTypeAll<RetainedGizmosWrapper>();
			if (instances.Length > 0) _instance = instances[0];
			if (_instance == null) {
				var go = new GameObject("RetainedGizmos") {
					hideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInInspector
				};
				_instance = go.AddComponent<RetainedGizmosWrapper>();
				if (Application.isPlaying) DontDestroyOnLoad(go);
			}
		}

		void OnEnable () {
			frameRedrawScope = new RetainedGizmos.RedrawScope(gizmos);
			Draw.builder = gizmos.GetBuilder(frameRedrawScope);
			commandBuffer = new CommandBuffer();
			commandBuffer.name = "DrawPro Gizmos";

			// Callback when rendering with the built-in render pipeline
			Camera.onPostRender += PostRender;
			// Callback when rendering with a scriptable render pipeline
			UnityEngine.Rendering.RenderPipelineManager.beginFrameRendering += BeginFrameRendering;
			UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += EndCameraRendering;
#if UNITY_EDITOR
			EditorApplication.update += OnUpdate;
			Selection.selectionChanged += OnSelectionChanged;
#endif
		}

		void BeginFrameRendering (ScriptableRenderContext context, Camera[] cameras) {
			gizmoVersion++;
		}

		void OnDisable () {
			Camera.onPostRender -= PostRender;
			UnityEngine.Rendering.RenderPipelineManager.beginFrameRendering -= BeginFrameRendering;
			UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= EndCameraRendering;
#if UNITY_EDITOR
			EditorApplication.update -= OnUpdate;
			Selection.selectionChanged -= OnSelectionChanged;
#endif
			Draw.builder.DiscardAndDispose();
			gizmos.ClearData();
		}

		private void OnApplicationQuit () {
			Draw.builder.DiscardAndDispose();
			gizmos.ClearData();
			Draw.builder = gizmos.GetBuilder(frameRedrawScope, true);
		}

		void OnSelectionChanged () {
		}

		void OnUpdate () {
			framePassed = true;
		}

		private void EndCameraRendering (ScriptableRenderContext context, Camera camera) {
			commandBuffer.Clear();
			SubmitFrame(camera, commandBuffer, true);
			context.ExecuteCommandBuffer(commandBuffer);
			// Note: required in the universal render pipeline but not in the HDRP since it calls submit later anyway
			context.Submit();
		}

		void PostRender (Camera camera) {
			commandBuffer.Clear();
			SubmitFrame(camera, commandBuffer, false);
			Graphics.ExecuteCommandBuffer(commandBuffer);
		}

		void SubmitFrame (Camera camera, CommandBuffer cmd, bool usingRenderPipeline) {
#if UNITY_EDITOR
			bool isSceneViewCamera = SceneView.currentDrawingSceneView != null && SceneView.currentDrawingSceneView.camera == camera;
#else
			bool isSceneViewCamera = false;
#endif
			// Do not include when rendering to a texture unless this is a scene view camera
			if (!(allowRenderToRenderTextures || drawToAllCameras) && camera.targetTexture != null && !isSceneViewCamera) return;

			// TODO: Test with multiple cameras
			if (Time.frameCount != lastFrameCount) {
				framePassed = true;
				lastFrameCount = Time.frameCount;
				previousFrameRedrawScope = frameRedrawScope;
				frameRedrawScope = new RetainedGizmos.RedrawScope(gizmos);
				Draw.builder.Dispose();
				Draw.builder = gizmos.GetBuilder(frameRedrawScope, true);
			} else if (framePassed && Application.isPlaying) {
				// Rendered frame passed without a game frame passing!
				// This might mean the game is paused.
				// Redraw gizmos while the game is paused.
				previousFrameRedrawScope.Draw();
			}

			if (framePassed) {
				gizmos.TickFrame();
				builtGizmos = false;
				framePassed = false;
			}

			Submit(camera, cmd, usingRenderPipeline);
		}

#if UNITY_EDITOR
		static System.Reflection.MethodInfo IsGizmosAllowedForObject = typeof(UnityEditor.EditorGUIUtility).GetMethod("IsGizmosAllowedForObject", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
		static System.Type AnnotationUtility = typeof(UnityEditor.PlayModeStateChange).Assembly?.GetType("UnityEditor.AnnotationUtility");
		System.Object[] cachedObjectParameterArray = new System.Object[1];
#endif

		bool use3dGizmos {
			get {
#if UNITY_EDITOR
				var use3dGizmosProperty = AnnotationUtility.GetProperty("use3dGizmos", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
				return (bool)use3dGizmosProperty.GetValue(null);
#else
				return true;
#endif
			}
		}

		Dictionary<System.Type, bool> typeToGizmosEnabled = new Dictionary<Type, bool>();

		bool ShouldDrawGizmos (UnityEngine.Object obj) {
#if UNITY_EDITOR
			// Use reflection to call EditorGUIUtility.IsGizmosAllowedForObject which is an internal method.
			// It is exactly the information we want though.
			// In case Unity has changed its API or something so that the method can no longer be found then just return true
			cachedObjectParameterArray[0] = obj;
			return IsGizmosAllowedForObject == null || (bool)IsGizmosAllowedForObject.Invoke(null, cachedObjectParameterArray);
#else
			return true;
#endif
		}

		void RemoveDestroyedGizmoDrawers () {
			for (int i = gizmoDrawers.Count - 1; i >= 0; i--) {
				var mono = gizmoDrawers[i] as MonoBehaviour;
				if (!mono || (mono.hideFlags & HideFlags.HideInHierarchy) == HideFlags.HideInHierarchy) {
					gizmoDrawers.RemoveAt(i);
				}
			}
		}

		void DrawGizmos (bool usingRenderPipeline) {
			UnityEngine.Profiling.Profiler.BeginSample("Refresh Selection Cache");
			GizmoContext.Refresh();
			UnityEngine.Profiling.Profiler.EndSample();
			UnityEngine.Profiling.Profiler.BeginSample("GizmosAllowed");
			typeToGizmosEnabled.Clear();
			if (!usingRenderPipeline) {
				// Fill the typeToGizmosEnabled dict with info about which classes should be drawn
				// We take advantage of the fact that IsGizmosAllowedForObject only depends on the type of the object and if it is active and enabled
				// and not the specific object instance.
				// When using a render pipeline the ShouldDrawGizmos method cannot be used becasue it seems to occationally crash Unity :(
				// However we can do better by using the GizmoVersion property on the IDrawGizmosWithVersion interface.
				// That can however not be used when not using rendering pipelines since the Unity gizmos are drawn after this class draws gizmos.
				// So we need these two separate cases.
				for (int i = gizmoDrawers.Count - 1; i >= 0; i--) {
					var tp = gizmoDrawers[i].GetType();
					if (!typeToGizmosEnabled.ContainsKey(tp) && (gizmoDrawers[i] as MonoBehaviour).isActiveAndEnabled) {
						typeToGizmosEnabled[tp] = ShouldDrawGizmos((UnityEngine.Object)gizmoDrawers[i]);
					}
				}
			}

			UnityEngine.Profiling.Profiler.EndSample();

			using (var gizmoBuilder = gizmos.GetBuilder()) {
				// Replace Draw.builder with a custom one just for gizmos
				var debugBuilder = Draw.builder;
				Draw.builder = gizmoBuilder;

				UnityEngine.Profiling.Profiler.BeginSample("DrawGizmos");
				GizmoContext.drawingGizmos = true;
				if (usingRenderPipeline) {
					for (int i = gizmoDrawers.Count - 1; i >= 0; i--) {
						if ((gizmoDrawers[i] as MonoBehaviour).isActiveAndEnabled) {
							try {
								gizmoDrawers[i].DrawGizmos();
							} catch (System.Exception e) {
								Debug.LogException(e, gizmoDrawers[i] as MonoBehaviour);
							}
						}
					}
					/*for (int i = gizmoDrawers.Count - 1; i >= 0; i--) {
					    if (!(gizmoDrawers[i] is IDrawGizmosWithVersion gizmoDrawerWithVersion) || gizmoDrawerWithVersion.GizmoVersion == gizmoVersion) {
					        try {
					            gizmoDrawers[i].DrawGizmos();
					        } catch (System.Exception e) {
					            Debug.LogException(e, gizmoDrawers[i] as MonoBehaviour);
					        }
					    }
					}*/
				} else {
					for (int i = gizmoDrawers.Count - 1; i >= 0; i--) {
						if ((gizmoDrawers[i] as MonoBehaviour).isActiveAndEnabled && typeToGizmosEnabled[gizmoDrawers[i].GetType()]) {
							try {
								gizmoDrawers[i].DrawGizmos();
							} catch (System.Exception e) {
								Debug.LogException(e, gizmoDrawers[i] as MonoBehaviour);
							}
						}
					}
				}
				GizmoContext.drawingGizmos = false;
				UnityEngine.Profiling.Profiler.EndSample();

				// Revert to the original builder
				Draw.builder = debugBuilder;
			}

			// Schedule jobs that may have been scheduled while drawing gizmos
			JobHandle.ScheduleBatchedJobs();
		}

		void Submit (Camera camera, CommandBuffer cmd, bool usingRenderPipeline) {
			// This must always be done to avoid a potential memory leak if gizmos are never drawn
			RemoveDestroyedGizmoDrawers();
#if UNITY_EDITOR
			bool drawGizmos = Handles.ShouldRenderGizmos() || drawToAllCameras;
#else
			bool drawGizmos = false;
#endif
			if (drawGizmos && !builtGizmos) {
				builtGizmos = true;
				DrawGizmos(usingRenderPipeline);
			}

			UnityEngine.Profiling.Profiler.BeginSample("Submit Gizmos");
			Draw.builder.Dispose();
			gizmos.Render(camera, drawGizmos, cmd);
			Draw.builder = gizmos.GetBuilder(frameRedrawScope, true);
			UnityEngine.Profiling.Profiler.EndSample();
		}

		public static void Register (IDrawGizmos item) {
			gizmoDrawers.Add(item);
		}
	}
}
