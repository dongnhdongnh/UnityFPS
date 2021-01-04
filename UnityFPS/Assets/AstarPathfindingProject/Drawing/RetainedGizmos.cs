using UnityEngine;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Rendering;
using System.Diagnostics;
using Unity.Jobs.LowLevel.Unsafe;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif
using System.Linq;

namespace Drawing {
	/// <summary>
	/// Helper for drawing Gizmos in a performant way.
	/// This is a replacement for the Unity Gizmos class as that is not very performant
	/// when drawing very large amounts of geometry (for example a large grid graph).
	/// These gizmos can be persistent, so if the data does not change, the gizmos
	/// do not need to be updated.
	///
	/// How to use
	/// - Create a Hasher object and hash whatever data you will be using to draw the gizmos
	///      Could be for example the positions of the vertices or something. Just as long as
	///      if the gizmos should change, then the hash changes as well.
	/// - Check if a cached mesh exists for that hash
	/// - If not, then create a Builder object and call the drawing methods until you are done
	///      and then call Finalize with a reference to a gizmos class and the hash you calculated before.
	/// - Call gizmos.Draw with the hash.
	/// - When you are done with drawing gizmos for this frame, call gizmos.FinalizeDraw
	///
	/// <code>
	/// var a = Vector3.zero;
	/// var b = Vector3.one;
	/// var color = Color.red;
	/// var hasher = RetainedGizmos.Create(this);
	/// hasher.AddHash(a.GetHashCode());
	/// hasher.AddHash(b.GetHashCode());
	/// hasher.AddHash(color.GetHashCode());
	/// if (!gizmos.Draw(hasher)) {
	///     using (var helper = gizmos.GetGizmoHelper(active, hasher)) {
	///         builder.DrawLine(a, b, color);
	///         builder.Finalize(gizmos, hasher);
	///     }
	/// }
	/// </code>
	/// </summary>
	public class RetainedGizmos {
		/// <summary>Combines hashes into a single hash value</summary>
		public struct Hasher : IEquatable<Hasher> {
			ulong hash;

			public static Hasher NotSupplied => new Hasher { hash = ulong.MaxValue };

			public static Hasher Create<T>(T init) {
				var h = new Hasher();

				h.Add(init);
				return h;
			}

			public void Add<T>(T hash) {
				// Just a regular hash function. The + 12289 is to make sure that hashing zeros doesn't just produce a zero (and generally that hashing one X doesn't produce a hash of X)
				// (with a struct we can't provide default initialization)
				this.hash = (1572869UL * this.hash) ^ (ulong)hash.GetHashCode() + 12289;
			}

			public ulong Hash {
				get {
					return hash;
				}
			}

			public override int GetHashCode () {
				return (int)hash;
			}

			public bool Equals (Hasher other) {
				return hash == other.hash;
			}
		}

		public struct RedrawScope {
			internal RetainedGizmos gizmos;
			/// <summary>
			/// ID of the scope.
			/// Zero means no or invalid scope.
			/// </summary>
			internal int id;

			static int idCounter = 1;

			public RedrawScope (RetainedGizmos gizmos) {
				this.gizmos = gizmos;
				// Should be enough with 4 billion ids before they wrap around.
				id = idCounter++;
			}

			/// <summary>
			/// Everything rendered with this scope and which is not older than one frame is drawn again.
			/// This is useful if you for some reason cannot draw some items during a frame (e.g. some asynchronous process is modifying the contents)
			/// but you still want to draw the same thing as the last frame to at least draw *something*.
			///
			/// Note: The items age will be reset. So the next frame you can call
			/// this method again to draw the items yet again.
			/// </summary>
			public void Draw () {
				if (gizmos != null) gizmos.Draw(this);
			}
		};

		internal struct ProcessedBuilderData {
			public enum Type {
				Invalid = 0,
				Static,
				Dynamic,
				Persistent,
				CustomMeshes,
			}

			public Type type;
			public BuilderData.Meta meta;
			bool submitted;
			public NativeArray<MeshBuffers> temporaryMeshBuffers;
			JobHandle buildJob, splitterJob;
			public List<MeshWithType> meshes;

			public bool isValid => type != Type.Invalid;

			public struct MeshBuffers {
				public UnsafeAppendBuffer splitterOutput, vertices, triangles, solidVertices, solidTriangles;
				public Bounds bounds;

				public MeshBuffers(Allocator allocator) {
					splitterOutput = new UnsafeAppendBuffer(0, 4, allocator);
					vertices = new UnsafeAppendBuffer(0, 4, allocator);
					triangles = new UnsafeAppendBuffer(0, 4, allocator);
					solidVertices = new UnsafeAppendBuffer(0, 4, allocator);
					solidTriangles = new UnsafeAppendBuffer(0, 4, allocator);
					bounds = new Bounds();
				}

				public void Dispose () {
					splitterOutput.Dispose();
					vertices.Dispose();
					triangles.Dispose();
					solidVertices.Dispose();
					solidTriangles.Dispose();
				}
			}

			public unsafe UnsafeAppendBuffer* splitterOutputPtr => & ((MeshBuffers*)temporaryMeshBuffers.GetUnsafePtr())->splitterOutput;

			public void Init (Type type, BuilderData.Meta meta) {
				submitted = false;
				this.type = type;
				this.meta = meta;

				if (meshes == null) meshes = new List<MeshWithType>();
				if (!temporaryMeshBuffers.IsCreated) {
					temporaryMeshBuffers = new NativeArray<MeshBuffers>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
					temporaryMeshBuffers[0] = new MeshBuffers(Allocator.Persistent);
				}
			}

			static int SubmittedJobs = 0;

			public void SetSplitterJob (RetainedGizmos gizmos, JobHandle splitterJob) {
				this.splitterJob = splitterJob;
				if (type == Type.Static) {
					unsafe {
						buildJob = CommandBuilder.Build(gizmos, (MeshBuffers*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(temporaryMeshBuffers), null, splitterJob);
					}

					SubmittedJobs++;
					// ScheduleBatchedJobs is expensive, so only do it once in a while
					if (SubmittedJobs % 8 == 0) {
						Profiler.BeginSample("ScheduleJobs");
						JobHandle.ScheduleBatchedJobs();
						Profiler.EndSample();
					}
				}
			}

			public void SchedulePersistFilter (int version, float time) {
				if (type != Type.Persistent) throw new System.InvalidOperationException();

				splitterJob.Complete();

				// If the command buffer is empty then this instance should not live longer
				if (temporaryMeshBuffers[0].splitterOutput.Size == 0) return;

				meta.version = version;
				// Guarantee that all drawing commands survive at least one frame
				// Don't filter them until we have drawn them once at least.
				if (submitted) {
					buildJob.Complete();
					unsafe {
						splitterJob = new CommandBuilder.PersistentFilterJob {
							buffer = &((MeshBuffers*)NativeArrayUnsafeUtility.GetUnsafePtr(temporaryMeshBuffers))->splitterOutput,
							time = time,
						}.Schedule(splitterJob);
					}
				}
			}

			public void Schedule (RetainedGizmos gizmos, Camera camera) {
				if (type != Type.Static) {
					unsafe {
						buildJob = CommandBuilder.Build(gizmos, (MeshBuffers*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(temporaryMeshBuffers), camera, splitterJob);
					}
				}
			}

			public void BuildMeshes (RetainedGizmos gizmos) {
				if ((type == Type.Static && submitted) || type == Type.CustomMeshes) return;
				buildJob.Complete();
				unsafe {
					PoolMeshes(gizmos);
					CommandBuilder.BuildMesh(gizmos, meshes, (MeshBuffers*)temporaryMeshBuffers.GetUnsafePtr());
				}
				submitted = true;
			}

			void PoolMeshes (RetainedGizmos gizmos) {
				if (type != Type.CustomMeshes) {
					for (int i = 0; i < meshes.Count; i++) gizmos.PoolMesh(meshes[i].mesh);
				}
				meshes.Clear();
			}

			public void Release (RetainedGizmos gizmos) {
				if (!isValid) throw new System.InvalidOperationException();
				type = Type.Invalid;
				splitterJob.Complete();
				buildJob.Complete();
				PoolMeshes(gizmos);
			}

			public void Dispose () {
				if (isValid) throw new System.InvalidOperationException();
				splitterJob.Complete();
				buildJob.Complete();
				if (temporaryMeshBuffers.IsCreated) {
					temporaryMeshBuffers[0].Dispose();
					temporaryMeshBuffers.Dispose();
				}
			}
		}

		internal struct BuilderData : IDisposable {
			public enum State {
				Free,
				Reserved,
				Initialized,
				WaitingForSplitter,
			}

			public struct Meta {
				public Hasher hasher;
				public RedrawScope redrawScope;
				public int version;
				public bool isGizmos;
			}

			public int uniqueID;
			public List<Mesh> meshes;
			public NativeArray<UnsafeAppendBuffer> commandBuffers;
			public State state { get; private set; }
			// TODO?
			public bool preventDispose;
			JobHandle splitterJob;
			public Meta meta;

			public void Reserve (int dataIndex) {
				if (state != State.Free) throw new System.InvalidOperationException();
				state = BuilderData.State.Reserved;
				uniqueID = dataIndex | (UniqueIDCounter++ << BuilderDataContainer.UniqueIDBitshift);
			}

			static int UniqueIDCounter = 0;

			public void Init (Hasher hasher, RedrawScope redrawScope, bool isGizmos) {
				if (state != State.Reserved) throw new System.InvalidOperationException();

				meta = new Meta {
					hasher = hasher,
					redrawScope = redrawScope,
					isGizmos = isGizmos,
					version = 0, // Will be filled in later
				};

				if (meshes == null) meshes = new List<Mesh>();
				if (!commandBuffers.IsCreated) {
					commandBuffers = new NativeArray<UnsafeAppendBuffer>(JobsUtility.MaxJobThreadCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
					for (int i = 0; i < commandBuffers.Length; i++) commandBuffers[i] = new UnsafeAppendBuffer(0, 4, Allocator.Persistent);
				}

				state = State.Initialized;
			}

			public unsafe UnsafeAppendBuffer* bufferPtr {
				get {
					return (UnsafeAppendBuffer*)commandBuffers.GetUnsafePtr();
				}
			}

			public void Submit (RetainedGizmos gizmos) {
				if (state != State.Initialized) throw new System.InvalidOperationException();

				meta.version = gizmos.version;

				// Command stream
				// split to static, dynamic and persistent
				// render static
				// render dynamic per camera
				// render persistent per camera
				int staticBuffer = gizmos.processedData.Reserve(ProcessedBuilderData.Type.Static, meta);
				int dynamicBuffer = gizmos.processedData.Reserve(ProcessedBuilderData.Type.Dynamic, meta);
				int persistentBuffer = gizmos.processedData.Reserve(ProcessedBuilderData.Type.Persistent, meta);

				unsafe {
					splitterJob = new CommandBuilder.StreamSplitter {
						inputBuffers = commandBuffers,
						staticBuffer = gizmos.processedData.Get(staticBuffer).splitterOutputPtr,
						dynamicBuffer = gizmos.processedData.Get(dynamicBuffer).splitterOutputPtr,
						persistentBuffer = gizmos.processedData.Get(persistentBuffer).splitterOutputPtr,
					}.Schedule();
				}

				gizmos.processedData.Get(staticBuffer).SetSplitterJob(gizmos, splitterJob);
				gizmos.processedData.Get(dynamicBuffer).SetSplitterJob(gizmos, splitterJob);
				gizmos.processedData.Get(persistentBuffer).SetSplitterJob(gizmos, splitterJob);

				if (meshes.Count > 0) {
					var customMeshes = gizmos.processedData.Get(gizmos.processedData.Reserve(ProcessedBuilderData.Type.CustomMeshes, meta)).meshes;
					// Copy meshes to render
					for (int i = 0; i < meshes.Count; i++) customMeshes.Add(new MeshWithType { mesh = meshes[i], lines = false });
					meshes.Clear();
				}

				// TODO: Allocate 3 output objects and pipe splitter to them

				// Only meshes valid for all cameras have been submitted.
				// Meshes that depend on the specific camera will be submitted just before rendering
				// that camera. Line drawing depends on the exact camera.
				// In particular when drawing circles different number of segments
				// are used depending on the distance to the camera.
				state = State.WaitingForSplitter;
			}

			public void Release () {
				if (state == State.Free) throw new System.InvalidOperationException();
				state = BuilderData.State.Free;
				ClearData();
			}

			void ClearData () {
				// Wait for any jobs that might be running
				// This is important to avoid memory corruption bugs
				splitterJob.Complete();
				meta = default;
				preventDispose = false;
				meshes.Clear();
				for (int i = 0; i < commandBuffers.Length; i++) {
					var buffer = commandBuffers[i];
					buffer.Reset();
					commandBuffers[i] = buffer;
				}
			}

			public void Dispose () {
				if (state == State.Reserved || state == State.Initialized) {
					UnityEngine.Debug.LogError("Drawing data is being destroyed, but a drawing instance is still active. Are you sure you have called Dispose on all drawing instances? This will cause a memory leak!");
					return;
				}

				splitterJob.Complete();
				if (commandBuffers.IsCreated) {
					for (int i = 0; i < commandBuffers.Length; i++) {
						commandBuffers[i].Dispose();
					}
					commandBuffers.Dispose();
				}
			}
		}

		internal struct BuilderDataContainer : IDisposable {
			BuilderData[] data;
			public const int UniqueIDBitshift = 16;
			const int IndexMask = (1 << UniqueIDBitshift) - 1;

			public int Reserve () {
				if (data == null) data = new BuilderData[1];
				for (int i = 0; i < data.Length; i++) {
					if (data[i].state == BuilderData.State.Free) {
						data[i].Reserve(i);
						return data[i].uniqueID;
					}
				}

				// Important to make ensure bitpacking doesn't collide
				if (data.Length * 2 > (1 << UniqueIDBitshift)) throw new System.Exception("Too many drawing instances active. Are some drawing instances not being disposed?");

				var newData = new BuilderData[data.Length * 2];
				data.CopyTo(newData, 0);
				data = newData;
				return Reserve();
			}

			public void Release (int uniqueID) {
				data[uniqueID & IndexMask].Release();
			}

			public bool StillExists (int uniqueID) {
				int index = uniqueID & IndexMask;

				if (data == null || index >= data.Length) return false;
				return data[index].uniqueID == uniqueID;
			}

			public ref BuilderData Get (int uniqueID) {
				int index = uniqueID & IndexMask;

				if (data[index].state == BuilderData.State.Free) throw new System.ArgumentException("Data is not reserved");
				if (data[index].uniqueID != uniqueID) throw new System.ArgumentException("This drawing instance has already been disposed");
				return ref data[index];
			}

			public void ReleaseAllUnused () {
				if (data == null) return;
				for (int i = 0; i < data.Length; i++) {
					if (data[i].state == BuilderData.State.WaitingForSplitter) {
						Release(i);
					}
				}
			}

			public void Dispose () {
				if (data != null) {
					for (int i = 0; i < data.Length; i++) data[i].Dispose();
				}
				// Ensures calling Dispose multiple times is a NOOP
				data = null;
			}
		}

		internal struct ProcessedBuilderDataContainer {
			ProcessedBuilderData[] data;
			Stack<int> freeSlots;

			public int Reserve (ProcessedBuilderData.Type type, BuilderData.Meta meta) {
				if (data == null) {
					data = new ProcessedBuilderData[0];
					freeSlots = new Stack<int>();
				}
				if (freeSlots.Count == 0) {
					var newData = new ProcessedBuilderData[math.max(4, data.Length*2)];
					data.CopyTo(newData, 0);
					for (int i = data.Length; i < newData.Length; i++) freeSlots.Push(i);
					data = newData;
				}
				int index = freeSlots.Pop();
				data[index].Init(type, meta);
				return index;
			}

			public ref ProcessedBuilderData Get (int index) {
				if (!data[index].isValid) throw new System.ArgumentException();
				return ref data[index];
			}

			void Release (RetainedGizmos gizmos, int i) {
				data[i].Release(gizmos);
				freeSlots.Push(i);
			}

			public void SubmitMeshes (RetainedGizmos gizmos, Camera camera, int versionThreshold, bool allowGizmos) {
				if (data == null) return;
				for (int i = 0; i < data.Length; i++) {
					if (data[i].isValid && data[i].meta.version >= versionThreshold && (allowGizmos || !data[i].meta.isGizmos)) {
						data[i].Schedule(gizmos, camera);
					}
				}
				for (int i = 0; i < data.Length; i++) {
					if (data[i].isValid && data[i].meta.version >= versionThreshold && (allowGizmos || !data[i].meta.isGizmos)) {
						data[i].BuildMeshes(gizmos);
					}
				}
			}

			public void CollectMeshes (int versionThreshold, List<MeshWithType> meshes, bool allowGizmos) {
				if (data == null) return;
				for (int i = 0; i < data.Length; i++) {
					if (data[i].isValid && data[i].meta.version >= versionThreshold && (allowGizmos || !data[i].meta.isGizmos)) {
						var itemMeshes = data[i].meshes;
						for (int j = 0; j < itemMeshes.Count; j++) {
							meshes.Add(itemMeshes[j]);
						}
					}
				}
			}

			public void FilterOldPersistentCommands (int version, float time) {
				if (data == null) return;
				for (int i = 0; i < data.Length; i++) {
					if (data[i].isValid && data[i].type == ProcessedBuilderData.Type.Persistent) {
						data[i].SchedulePersistFilter(version, time);
					}
				}
			}

			public bool SetVersion (Hasher hasher, int version) {
				if (data == null) return false;
				bool found = false;

				for (int i = 0; i < data.Length; i++) {
					if (data[i].isValid && data[i].meta.hasher.Hash == hasher.Hash) {
						data[i].meta.version = version;
						found = true;
					}
				}
				return found;
			}

			public bool SetVersion (RedrawScope scope, int version) {
				if (data == null) return false;
				bool found = false;

				for (int i = 0; i < data.Length; i++) {
					if (data[i].isValid && data[i].meta.redrawScope.id == scope.id) {
						data[i].meta.version = version;
						found = true;
					}
				}
				return found;
			}

			public void ReleaseDataOlderThan (RetainedGizmos gizmos, int version) {
				if (data == null) return;
				for (int i = 0; i < data.Length; i++) {
					if (data[i].isValid && data[i].meta.version < version) {
						Release(gizmos, i);
					}
				}
			}

			public void ReleaseAllWithHash (RetainedGizmos gizmos, Hasher hasher) {
				if (data == null) return;
				for (int i = 0; i < data.Length; i++) {
					if (data[i].isValid && data[i].meta.hasher.Hash == hasher.Hash) {
						Release(gizmos, i);
					}
				}
			}

			public void Dispose (RetainedGizmos gizmos) {
				if (data == null) return;
				for (int i = 0; i < data.Length; i++) {
					if (data[i].isValid) Release(gizmos, i);
					data[i].Dispose();
				}
				// Ensures calling Dispose multiple times is a NOOP
				data = null;
			}
		}

		internal struct MeshWithType {
			public Mesh mesh;
			public bool lines;
		}

		internal BuilderDataContainer data;
		internal ProcessedBuilderDataContainer processedData;
		List<MeshWithType> meshes = new List<MeshWithType>();
		Stack<Mesh> cachedMeshes = new Stack<Mesh>();

		void PoolMesh (Mesh mesh) {
			mesh.Clear();
			cachedMeshes.Push(mesh);
		}

		internal Mesh GetMesh () {
			if (cachedMeshes.Count > 0) {
				return cachedMeshes.Pop();
			} else {
				var mesh = new Mesh {
					hideFlags = HideFlags.DontSave
				};
				mesh.MarkDynamic();
				return mesh;
			}
		}

		public CommandBuilder GetBuilder (bool isGizmos = true) {
			return new CommandBuilder(this, Hasher.NotSupplied, default, isGizmos);
		}

		public CommandBuilder GetBuilder (RedrawScope redrawScope, bool isGizmos = true) {
			return new CommandBuilder(this, Hasher.NotSupplied, redrawScope, isGizmos);
		}

		public CommandBuilder GetBuilder (Hasher hasher, RedrawScope redrawScope = default, bool isGizmos = true) {
			// The user is going to rebuild the data with the given hash
			// Let's clear the previous data with that hash since we know it is not needed any longer.
			// Do not do this if a hash is not given.
			if (!hasher.Equals(Hasher.NotSupplied)) DiscardData(hasher);
			return new CommandBuilder(this, hasher, redrawScope, isGizmos);
		}

		/// <summary>Material to use for the navmesh in the editor</summary>
		public Material surfaceMaterial;

		/// <summary>Material to use for the navmesh outline in the editor</summary>
		public Material lineMaterial;

		public int version { get; private set; } = 1;
		int lastTickVersion;
		int lastTickVersion2;

		struct Range {
			public int start;
			public int end;
		}

		Dictionary<Camera, Range> cameraVersions = new Dictionary<Camera, Range>();

		void DiscardData (Hasher hasher) {
			processedData.ReleaseAllWithHash(this, hasher);
		}

		/// <summary>
		/// Schedules the meshes for the specified hash to be drawn.
		/// Returns: False if there is no cached mesh for this hash, you may want to
		///  submit one in that case. The draw command will be issued regardless of the return value.
		/// </summary>
		public bool Draw (Hasher hasher) {
			if (hasher.Equals(Hasher.NotSupplied)) throw new System.ArgumentException("Invalid hash value");
			return processedData.SetVersion(hasher, version);
		}

		/// <summary>Schedules all meshes that were drawn the last frame with this redraw scope to be drawn again</summary>
		public void Draw (RedrawScope scope) {
			if (scope.id != 0) processedData.SetVersion(scope, version);
		}

		public void TickFrame () {
			// All cameras rendered between the last tick and this one will have
			// a version at least lastTickVersion + 1.
			// However the user may want to reuse meshes from the previous frame (see Draw(Hasher)).
			// This requires us to keep data from one more frame and thus we use lastTickVersion2 + 1
			// TODO: One frame should be enough, right?
			data.ReleaseAllUnused();
			processedData.FilterOldPersistentCommands(version, Time.time);
			processedData.ReleaseDataOlderThan(this, lastTickVersion2 + 1);
			lastTickVersion2 = lastTickVersion;
			lastTickVersion = version;
			// TODO: Filter cameraVersions to avoid memory leak
		}

		Plane[] frustrumPlanes = new Plane[6];

		/// <summary>Call after all <see cref="Draw"/> commands for the frame have been done to draw everything</summary>
		public void Render (Camera cam, bool allowGizmos, CommandBuffer commandBuffer) {
			Profiler.BeginSample("Draw Retained Gizmos");

#if UNITY_EDITOR
			// Make sure the material references are correct
			if (surfaceMaterial == null) surfaceMaterial = Resources.Load<Material>("astar_navmesh_surface");
			if (lineMaterial == null) lineMaterial = Resources.Load<Material>("astar_navmesh_outline");
#endif

			var planes = frustrumPlanes;
			GeometryUtility.CalculateFrustumPlanes(cam, planes);

			// Silently do nothing if the materials are not set
			if (surfaceMaterial == null || lineMaterial == null) return;

			if (!cameraVersions.TryGetValue(cam, out Range cameraRenderingRange)) {
				cameraRenderingRange = new Range { start = int.MinValue, end = int.MinValue };
			}

			// Check if the last time the camera was rendered
			// was during the current frame.
			if (cameraRenderingRange.end > lastTickVersion) {
				// In some cases a camera is rendered multiple times per frame.
				// In this case we only extend the drawing range up to the current version.
				// The reasoning is that all times the camera is rendered in a frame
				// all things should be drawn.
				// If we did update the version gizmos etc. would only be drawn
				// the first time the camera was rendered in a frame.

				// Sometimes the scene view will be rendered twice in a single frame
				// due to some internal Unity tooltip code.
				// Without this fix the scene view camera may end up showing no gizmos
				// for a single frame.
				cameraRenderingRange.end = version + 1;
			} else {
				// This is the common case: the previous time the camera was rendered
				// it rendered all versions lower than cameraRenderingRange.end.
				// So now we start by rendering from that version.
				cameraRenderingRange = new Range  { start = cameraRenderingRange.end, end = version + 1 };
			}

			// Don't show anything rendered before the last frame.
			// If the camera has been turned off for a while and then suddenly starts rendering again
			// we want to make sure that we don't render meshes from multiple frames.
			// This happens often in the unity editor as the scene view and game view often skip
			// rendering many frames when outside of play mode.
			cameraRenderingRange.start = Mathf.Max(cameraRenderingRange.start, lastTickVersion2 + 1);

			// If GL.wireframe is enabled (the Wireframe mode in the scene view settings)
			// then I have found no way to draw gizmos in a good way.
			// It's best to disable gizmos altogether to avoid drawing wireframe versions of gizmo meshes.
			if (!GL.wireframe) {
				Profiler.BeginSample("Build Meshes");
				processedData.SubmitMeshes(this, cam, cameraRenderingRange.start, allowGizmos);
				Profiler.EndSample();
				Profiler.BeginSample("Collect Meshes");
				meshes.Clear();
				processedData.CollectMeshes(cameraRenderingRange.start, meshes, allowGizmos);
				Profiler.EndSample();

				// First surfaces, then lines
				for (int matIndex = 0; matIndex <= 1; matIndex++) {
					var mat = matIndex == 0 ? surfaceMaterial : lineMaterial;
					for (int pass = 0; pass < mat.passCount; pass++) {
						for (int i = 0; i < meshes.Count; i++) {
							if (meshes[i].lines == (mat == lineMaterial) && GeometryUtility.TestPlanesAABB(planes, meshes[i].mesh.bounds)) {
								commandBuffer.DrawMesh(meshes[i].mesh, Matrix4x4.identity, mat, 0, pass, null);
							}
						}
					}
				}

				meshes.Clear();
			}

			cameraVersions[cam] = cameraRenderingRange;
			version++;
			Profiler.EndSample();
		}

		/// <summary>
		/// Destroys all cached meshes.
		/// Used to make sure that no memory leaks happen in the Unity Editor.
		/// </summary>
		public void ClearData () {
			data.Dispose();
			processedData.Dispose(this);

			while (cachedMeshes.Count > 0) {
				Mesh.DestroyImmediate(cachedMeshes.Pop());
			}

			UnityEngine.Assertions.Assert.IsTrue(meshes.Count == 0);
		}
	}
}
