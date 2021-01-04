//#define DEBUG_JOBS
namespace Pathfinding.Jobs {
	using System.Reflection;
	using Unity.Collections;
	using Unity.Jobs;
	using Unity.Burst;
	using System.Collections.Generic;
	using Unity.Collections.LowLevel.Unsafe;
	using Pathfinding.Util;
	using Unity.Mathematics;
	using System.Threading;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Disable the check that prevents jobs from including uninitialized native arrays open for reading.
	///
	/// Sometimes jobs have to include a readable native array that starts out uninitialized.
	/// The job might for example write to it and later read from it in the same job.
	///
	/// See: <see cref="JobDependencyHandler.NewNativeArray"/>
	/// </summary>
	class DisableUninitializedReadCheckAttribute : System.Attribute {
	}

	/// <summary>Very simple list based on NativeList</summary>
	struct NativeList<T> where T : struct {
		public int count;
		public NativeArray<T> data;

		public void Add (T item) {
			if (count == data.Length || !data.IsCreated) Memory.Realloc(ref data, math.ceilpow2(count+1), Allocator.Persistent, NativeArrayOptions.ClearMemory);
			data[count] = item;
			count++;
		}

		public void Clear () {
			count = 0;
			if (data.IsCreated) data.Dispose();
		}
	}

	struct NativeArrayArena {
		List<NativeArray<byte> > buffer;

		public void Add<T>(NativeArray<T> data) where T : struct {
			if (buffer == null) buffer = ListPool<NativeArray<byte> >.Claim();
			buffer.Add(data.Reinterpret<byte>(UnsafeUtility.SizeOf<T>()));
		}

		public void DisposeAll () {
			if (buffer == null) return;

			UnityEngine.Profiling.Profiler.BeginSample("Disposing");
			for (int i = 0; i < buffer.Count; i++) buffer[i].Dispose();
			ListPool<NativeArray<byte> >.Release(ref buffer);
			UnityEngine.Profiling.Profiler.EndSample();
		}
	}

	/// <summary>
	/// Automatic dependency tracking for the Unity Job System.
	///
	/// Uses reflection to find the [ReadOnly] and [WriteOnly] attributes on job data struct fields.
	/// These are used to automatically figure out dependencies between jobs.
	///
	/// A job that reads from an array depends on the last job that wrote to that array.
	/// A job that writes to an array depends on the last job that wrote to the array as well as all jobs that read from the array.
	///
	/// <code>
	/// struct ExampleJob : IJob {
	///     public NativeArray<int> someData;
	///
	///     public void Execute () {
	///         // Do something
	///     }
	/// }
	///
	/// void Start () {
	///     var tracker = new JobDependencyTracker();
	///     var data = new NativeArray<int>(100, Allocator.TempJob);
	///     var job1 = new ExampleJob {
	///         someData = data
	///     }.Schedule(tracker);
	///
	///     var job2 = new ExampleJob {
	///         someData = data
	///     }.Schedule(tracker);
	///
	///     // job2 automatically depends on job1 because they both require read/write access to the data array
	/// }
	/// </code>
	///
	/// See: <see cref="Pathfinding.Util.IJobExtensions"/>
	/// </summary>
	public class JobDependencyTracker : IAstarPooledObject {
		internal List<NativeArraySlot> slots = ListPool<NativeArraySlot>.Claim();
		List<MainThreadWork> mainThreadWork = ListPool<MainThreadWork>.Claim();
		NativeList<JobHandle> temporaryJobs;
		List<GCHandle> gcHandlesToFree;
		NativeArrayArena arena;
		public bool forceLinearDependencies { get; private set; }

		struct MainThreadWork {
			public JobHandle dependsOn;
			public IJob job;
			public ManualResetEvent doneEvent;
			public ManualResetEvent dependenciesDoneEvent;

			public void Complete () {
				JobHandle.ScheduleBatchedJobs();
				// Calling 'Complete' seems to cause very rare deadlocks for unknown reasons.
				// Seems like it is a Unity bug. A slightly less efficient solution is to use the dependenciesDoneEvent.
				// This seems to work without any rare deadlocks.
				//dependsOn.Complete();
				dependenciesDoneEvent.WaitOne();
				UnityEngine.Profiling.Profiler.BeginSample("Main thread job");
				try {
					job.Execute();
				} finally {
					UnityEngine.Profiling.Profiler.EndSample();
					doneEvent.Set();
				}
			}

			public bool RunIfReady () {
				//if (dependsOn.IsCompleted) {
				if (dependenciesDoneEvent.WaitOne(0)) {
					Complete();
					return true;
				}
				return false;
			}
		}

		internal struct JobInstance {
			public JobHandle handle;
			public int hash;
			#if DEBUG_JOBS
			public string name;
			#endif
		}

		internal struct NativeArraySlot {
			public long hash;
			public JobInstance lastWrite;
			public List<JobInstance> lastReads;
			public bool initialized;
			public bool hasWrite;
		}

		// Note: burst compiling even an empty job can avoid the overhead of going from unmanaged to managed code.
		/* [BurstCompile]
		struct JobDispose<T> : IJob where T : struct {
		    [DeallocateOnJobCompletion]
		    [DisableUninitializedReadCheck]
		    public NativeArray<T> data;

		    public void Execute () {
		    }
		}*/

		struct JobRaycastCommandDummy : IJob {
			[ReadOnly]
			public NativeArray<UnityEngine.RaycastCommand> commands;
			[WriteOnly]
			public NativeArray<UnityEngine.RaycastHit> results;

			public void Execute () {}
		}

		/// <summary>
		/// JobHandle that represents a dependency for all jobs.
		/// All native arrays that are written (and have been tracked by this tracker) to will have their final results in them
		/// when the returned job handle is complete.
		/// </summary>
		public JobHandle AllWritesDependency {
			get {
				var handles = new NativeArray<JobHandle>(slots.Count, Allocator.Temp);
				for (int i = 0; i < slots.Count; i++) handles[i] = slots[i].lastWrite.handle;
				var dependencies = JobHandle.CombineDependencies(handles);
				handles.Dispose();
				return dependencies;
			}
		}

		/// <summary>
		/// Disable dependency tracking and just run jobs one after the other.
		/// This may be faster in some cases since dependency tracking has some overhead.
		/// </summary>
		public void ForceLinearDependencies () {
			AllWritesDependency.Complete();
			forceLinearDependencies = true;
		}

		public NativeArray<T> NewNativeArray<T>(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : struct {
			var res = new NativeArray<T>(length, allocator, options);
			unsafe {
				slots.Add(new NativeArraySlot {
					hash = (long)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(res),
					lastWrite = default,
					lastReads = ListPool<JobInstance>.Claim(),
					initialized = options == NativeArrayOptions.ClearMemory,
				});
			}
			arena.Add(res);
			return res;
		}

		/// <summary>
		/// Schedules a raycast batch command.
		/// Like RaycastCommand.ScheduleBatch, but dependencies are tracked automatically.
		/// </summary>
		public JobHandle ScheduleBatch (NativeArray<UnityEngine.RaycastCommand> commands, NativeArray<UnityEngine.RaycastHit> results, int minCommandsPerJob) {
			if (forceLinearDependencies) {
				UnityEngine.RaycastCommand.ScheduleBatch(commands, results, minCommandsPerJob).Complete();
				return default;
			}

			// Create a dummy structure to allow the analyzer to determine how the job reads/writes data
			var dummy = new JobRaycastCommandDummy { commands = commands, results = results };
			var dependencies = JobDependencyAnalyzer<JobRaycastCommandDummy>.GetDependencies(ref dummy, slots);
			var job = UnityEngine.RaycastCommand.ScheduleBatch(commands, results, minCommandsPerJob, dependencies);

			JobDependencyAnalyzer<JobRaycastCommandDummy>.Scheduled(ref dummy, this, job);
			return job;
		}

		/* Disposes the native array after all the current jobs are finished with it */
		//public void DeferDispose<T>(NativeArray<T> data) where T : struct {
		//temporaryJobs.Add(new JobDispose<T> { data = data }.Schedule(this));
		//}

		/// <summary>Frees the GCHandle when the JobDependencyTracker is disposed</summary>
		public void DeferFree (GCHandle handle, JobHandle dependsOn) {
			if (gcHandlesToFree == null) gcHandlesToFree = ListPool<GCHandle>.Claim();
			gcHandlesToFree.Add(handle);
		}

		public void CompleteMainThreadWork () {
			if (mainThreadWork != null) {
				for (int i = 0; i < mainThreadWork.Count; i++) {
					mainThreadWork[i].Complete();
				}
				mainThreadWork.Clear();
			}
		}

		public void RunMainThreadWork () {
			if (mainThreadWork != null) {
				for (int i = 0; i < mainThreadWork.Count; i++) {
					if (mainThreadWork[i].RunIfReady()) {
						// Remove this work item
						mainThreadWork.RemoveAt(i);
						i --;
					}
				}
			}
		}

		internal void AddMainThreadWork (IJob job, ManualResetEvent doneEvent, ManualResetEvent dependenciesDoneEvent, JobHandle dependsOn) {
			mainThreadWork.Add(new MainThreadWork {
				dependsOn = dependsOn,
				dependenciesDoneEvent = dependenciesDoneEvent,
				doneEvent = doneEvent,
				job = job,
			});
		}

		#if DEBUG_JOBS
		internal void JobReadsFrom (JobHandle job, long nativeArrayHash, int jobHash, string jobName)
		#else
		internal void JobReadsFrom (JobHandle job, long nativeArrayHash, int jobHash)
		#endif
		{
			for (int j = 0; j < slots.Count; j++) {
				var slot = slots[j];
				if (slot.hash == nativeArrayHash) {
					// If the job only reads from the array then we just add this job to the list of readers
					slot.lastReads.Add(new JobInstance {
						handle = job,
						hash = jobHash,
						#if DEBUG_JOBS
						name = jobName,
						#endif
					});
					break;
				}
			}
		}

		#if DEBUG_JOBS
		internal void JobWritesTo (JobHandle job, long nativeArrayHash, int jobHash, string jobName)
		#else
		internal void JobWritesTo (JobHandle job, long nativeArrayHash, int jobHash)
		#endif
		{
			for (int j = 0; j < slots.Count; j++) {
				var slot = slots[j];
				if (slot.hash == nativeArrayHash) {
					// If the job writes to the array then this job is now the last writer
					slot.lastWrite = new JobInstance {
						handle = job,
						hash = jobHash,
						#if DEBUG_JOBS
						name = jobName,
						#endif
					};
					slot.lastReads.Clear();
					// The array no longer contains uninitialized data.
					// Parts of it may still be uninitialized if the job doesn't write to everything, but that's something that this class cannot track.
					slot.initialized = true;
					slot.hasWrite = true;
					slots[j] = slot;
					break;
				}
			}
		}

		/// <summary>
		/// Diposes this tracker.
		/// This will pool all used lists which makes the GC happy.
		///
		/// Note: It is necessary to call this method to avoid memory leaks if you are using the DeferDispose method. But it's a good thing to do otherwise as well.
		/// It is automatically called if you are using the ObjectPool<T>.Release method.
		/// </summary>
		void Dispose () {
			if (mainThreadWork.Count > 0) throw new System.InvalidOperationException("Cannot pool the JobDependencyTracker while there are still main thread work items to complete. Call CompleteMainThreadWork() first.");
			for (int i = 0; i < slots.Count; i++) ListPool<JobInstance>.Release(slots[i].lastReads);

			// Need to call complete on e.g. dispose jobs, otherwise these will create small memory leaks due to the handles being kept allocated
			for (int i = 0; i < temporaryJobs.count; i++) temporaryJobs.data[i].Complete();
			temporaryJobs.Clear();

			if (gcHandlesToFree != null) {
				for (int i = 0; i < gcHandlesToFree.Count; i++) gcHandlesToFree[i].Free();
				ListPool<GCHandle>.Release(ref gcHandlesToFree);
			}

			slots.Clear();
			mainThreadWork.Clear();
			arena.DisposeAll();
			forceLinearDependencies = false;
		}

		void IAstarPooledObject.OnEnterPool () {
			Dispose();
		}
	}

	/// <summary>Extension methods for IJob and related interfaces</summary>
	public static class IJobExtensions {
		struct ManagedJob : IJob {
			public GCHandle handle;

			public void Execute () {
				((IJob)handle.Target).Execute();
				handle.Free();
			}
		}

		struct ManagedActionJob : IJob {
			public GCHandle handle;

			public void Execute () {
				((System.Action)handle.Target)();
				handle.Free();
			}
		}

		struct ManagedJobParallelForBatch : IJobParallelForBatched {
			public GCHandle handle;

			public bool allowBoundsChecks => false;

			public void Execute (int startIndex, int count) {
				((Pathfinding.Jobs.IJobParallelForBatched)handle.Target).Execute(startIndex, count);
			}
		}

		/// <summary>
		/// Schedule a job and handle dependencies automatically.
		/// You need to have "using Pathfinding.Util" in your script to be able to use this extension method.
		///
		/// See: <see cref="Pathfinding.Util.JobDependencyTracker"/>
		/// </summary>
		public static JobHandle Schedule<T>(this T data, JobDependencyTracker tracker) where T : struct, IJob {
			if (tracker.forceLinearDependencies) {
				data.Run();
				return default;
			} else {
				var job = data.Schedule(JobDependencyAnalyzer<T>.GetDependencies(ref data, tracker.slots));
				JobDependencyAnalyzer<T>.Scheduled(ref data, tracker, job);
				return job;
			}
		}

		public static JobHandle ScheduleBatch<T>(this T data, int arrayLength, int minIndicesPerJobCount, JobDependencyTracker tracker, JobHandle additionalDependency = default) where T : struct, IJobParallelForBatched {
			if (tracker.forceLinearDependencies) {
				//data.ScheduleBatch(arrayLength, minIndicesPerJobCount, additionalDependency).Complete();
				data.RunBatch(arrayLength);
				return default;
			} else {
				var job = data.ScheduleBatch(arrayLength, minIndicesPerJobCount, JobDependencyAnalyzer<T>.GetDependencies(ref data, tracker.slots, additionalDependency));

				JobDependencyAnalyzer<T>.Scheduled(ref data, tracker, job);
				return job;
			}
		}

		public static JobHandle ScheduleManaged<T>(this T data, JobHandle dependsOn) where T : struct, IJob {
			return new ManagedJob { handle = GCHandle.Alloc(data) }.Schedule(dependsOn);
		}

		public static JobHandle ScheduleManaged (this System.Action data, JobHandle dependsOn) {
			return new ManagedActionJob {
					   handle = GCHandle.Alloc(data)
			}.Schedule(dependsOn);
		}

		public static JobHandle GetDependencies<T>(this T data, JobDependencyTracker tracker) where T : struct, IJob {
			return JobDependencyAnalyzer<T>.GetDependencies(ref data, tracker.slots);
		}


		static readonly UnityEngine.Profiling.CustomSampler waitingForMainThreadSampler = UnityEngine.Profiling.CustomSampler.Create("Waiting for main thread work (sleep)");

		/// <summary>
		/// Schedules a job to run in the main Unity thread.
		///
		/// You must call <see cref="JobDependencyTracker.RunMainThreadWork()"/> repeatedly in the main thread to allow the work to be done.
		///
		/// Warning: This method is to be avoided unless absolutely necessary. It may cause a worker thread to sleep until the main thread has had time to run the job, preventing other jobs from running in that worker thread.
		///
		/// Note: In the future Unity may allow more direct control over the semaphores used in the job system, and then this method may become more efficient.
		/// </summary>
		public static JobHandle ScheduleManagedInMainThread<T>(this T data, JobDependencyTracker tracker) where T : struct, IJob {
			if (tracker.forceLinearDependencies) {
				UnityEngine.Profiling.Profiler.BeginSample("Main Thread Work");
				data.Execute();
				UnityEngine.Profiling.Profiler.EndSample();
				return default;
			}

			var ijob = (IJob)data;
			var dependsOn = JobDependencyAnalyzer<T>.GetDependencies(ref data, tracker.slots);
			var dependenciesDoneEvent = new ManualResetEvent(false);
			var doneEvent = new ManualResetEvent(false);

			tracker.AddMainThreadWork(ijob, doneEvent, dependenciesDoneEvent, dependsOn);

			System.Action waitAction = () => {
				waitingForMainThreadSampler.Begin();
				dependenciesDoneEvent.Set();
				doneEvent.WaitOne();
				waitingForMainThreadSampler.End();
			};
			var waitJob = waitAction.ScheduleManaged(dependsOn);

			JobDependencyAnalyzer<T>.Scheduled(ref data, tracker, waitJob);
			return waitJob;
		}
	}

	internal static class JobDependencyAnalyzerAssociated {
		internal static UnityEngine.Profiling.CustomSampler getDependenciesSampler = UnityEngine.Profiling.CustomSampler.Create("GetDependencies");
		internal static UnityEngine.Profiling.CustomSampler iteratingSlotsSampler = UnityEngine.Profiling.CustomSampler.Create("IteratingSlots");
		internal static UnityEngine.Profiling.CustomSampler initSampler = UnityEngine.Profiling.CustomSampler.Create("Init");
		internal static UnityEngine.Profiling.CustomSampler combineSampler = UnityEngine.Profiling.CustomSampler.Create("Combining");
		internal static int[] tempJobDependencyHashes = new int[16];
		internal static int jobCounter = 1;
	}

	struct JobDependencyAnalyzer<T> where T : struct {
		static ReflectionData reflectionData;
		struct ReflectionData {
			public int[] fieldOffsets;
			public bool[] writes;
			public bool[] checkUninitializedRead;
			public string[] fieldNames;

			public void Build () {
				// Find the byte offsets within the struct to all m_Buffer fields in all the native arrays in the struct
				var bufferOffset = UnsafeUtility.GetFieldOffset(typeof(NativeArray<>).GetField("m_Buffer", BindingFlags.Instance | BindingFlags.NonPublic));
				var fields = new List<int>();
				var writes = new List<bool>();
				var reads = new List<bool>();
				var names = new List<string>();

				Build(typeof(T), fields, writes, reads, names, bufferOffset, false, false, false);
				this.fieldOffsets = fields.ToArray();
				this.writes = writes.ToArray();
				this.fieldNames = names.ToArray();
				this.checkUninitializedRead = reads.ToArray();
			}

			void Build (System.Type type, List<int> fields, List<bool> writes, List<bool> reads, List<string> names, int offset, bool forceReadOnly, bool forceWriteOnly, bool forceDisableUninitializedCheck) {
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					// Check if this field is a NativeArray
					if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(NativeArray<>)) {
						fields.Add(offset + UnsafeUtility.GetFieldOffset(field));
						writes.Add(!forceReadOnly && field.GetCustomAttribute(typeof(ReadOnlyAttribute)) == null);
						reads.Add(!forceWriteOnly && !forceDisableUninitializedCheck && field.GetCustomAttribute(typeof(WriteOnlyAttribute)) == null && field.GetCustomAttribute(typeof(DisableUninitializedReadCheckAttribute)) == null);
						names.Add(field.Name);
					} else if (!field.FieldType.IsPrimitive && field.FieldType.IsValueType && !field.FieldType.IsEnum) {
						// Recurse to handle nested types
						bool readOnly = field.GetCustomAttribute(typeof(ReadOnlyAttribute)) != null;
						bool writeOnly = field.GetCustomAttribute(typeof(WriteOnlyAttribute)) != null;
						bool disableUninitializedCheck = field.GetCustomAttribute(typeof(DisableUninitializedReadCheckAttribute)) != null;
						Build(field.FieldType, fields, writes, reads, names, offset + UnsafeUtility.GetFieldOffset(field), readOnly, writeOnly, disableUninitializedCheck);
					}
				}
			}
		}

		static void initReflectionData () {
			if (reflectionData.fieldOffsets == null) {
				reflectionData.Build();
			}
		}

		static bool HasHash (int[] hashes, int hash, int count) {
			for (int i = 0; i < count; i++) if (hashes[i] == hash) return true;
			return false;
		}

		/// <summary>Returns the dependencies for the given job.</summary>
		/// <param name="data">Job data. Must be allocated on the stack.</param>
		public static JobHandle GetDependencies (ref T data, List<JobDependencyTracker.NativeArraySlot> slots) {
			return GetDependencies(ref data, slots, default, false);
		}

		public static JobHandle GetDependencies (ref T data, List<JobDependencyTracker.NativeArraySlot> slots, JobHandle additionalDependency) {
			return GetDependencies(ref data, slots, additionalDependency, true);
		}

		static JobHandle GetDependencies (ref T data, List<JobDependencyTracker.NativeArraySlot> slots, JobHandle additionalDependency, bool useAdditionalDependency) {
			//JobDependencyAnalyzerAssociated.getDependenciesSampler.Begin();
			//JobDependencyAnalyzerAssociated.initSampler.Begin();
			var dependencies = new NativeArray<JobHandle>(16, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			var dependencyHashes = JobDependencyAnalyzerAssociated.tempJobDependencyHashes;

			int numDependencies = 0;

			//JobDependencyAnalyzerAssociated.initSampler.End();
			initReflectionData();
			#if DEBUG_JOBS
			string dependenciesDebug = "";
			#endif
			unsafe {
				// Note: data is a struct. It is stored on the stack and can thus not be moved by the GC.
				// Therefore we do not need to pin it first.
				// It is guaranteed to be stored on the stack since the Schedule method takes the data parameter by value and not by reference.
				byte* dataPtr = (byte*)UnsafeUtility.AddressOf(ref data);

				for (int i = 0; i < reflectionData.fieldOffsets.Length; i++) {
					// This is the internal value of the m_Buffer field of the NativeArray
					void* nativeArrayBufferPtr = *(void**)(dataPtr + reflectionData.fieldOffsets[i]);

					// Use the pointer as a hash to uniquely identify a NativeArray
					var hash = (long)nativeArrayBufferPtr;

					//JobDependencyAnalyzerAssociated.iteratingSlotsSampler.Begin();
					for (int j = 0; j <= slots.Count; j++) {
						// No slot found. Add a new one
						if (j == slots.Count) {
							slots.Add(new JobDependencyTracker.NativeArraySlot {
								hash = hash,
								lastWrite = default,
								lastReads = ListPool<JobDependencyTracker.JobInstance>.Claim(),
								initialized = true, // We don't know anything about the array, so assume it contains initialized data. JobDependencyTracker.NewNativeArray should be used otherwise.
								hasWrite = false,
							});
						}

						// Check if we know about this NativeArray yet
						var slot = slots[j];
						if (slot.hash == hash) {
							if (reflectionData.checkUninitializedRead[i] && !slot.initialized) {
								throw new System.InvalidOperationException("A job tries to read from the native array " + typeof(T).Name + "." + reflectionData.fieldNames[i] + " which contains uninitialized data");
							}

							if (slot.hasWrite && !HasHash(dependencyHashes, slot.lastWrite.hash, numDependencies)) {
								// Reads/writes always depend on the last write to the native array
								dependencies[numDependencies] = slot.lastWrite.handle;
								dependencyHashes[numDependencies] = slot.lastWrite.hash;
								numDependencies++;
								if (numDependencies >= dependencies.Length) throw new System.Exception("Too many dependencies for job");
								#if DEBUG_JOBS
								dependenciesDebug += slot.lastWrite.name + " ";
								#endif
							}

							// If we want to write to the array we additionally depend on all previous reads of the array
							if (reflectionData.writes[i]) {
								for (int q = 0; q < slot.lastReads.Count; q++) {
									if (!HasHash(dependencyHashes, slot.lastReads[q].hash, numDependencies)) {
										dependencies[numDependencies] = slot.lastReads[q].handle;
										dependencyHashes[numDependencies] = slot.lastReads[q].hash;
										numDependencies++;
										if (numDependencies >= dependencies.Length) throw new System.Exception("Too many dependencies for job");
										#if DEBUG_JOBS
										dependenciesDebug += slot.lastReads[q].name + " ";
										#endif
									}
								}
							}
							break;
						}
					}
					//JobDependencyAnalyzerAssociated.iteratingSlotsSampler.End();
				}

				if (useAdditionalDependency) {
					dependencies[numDependencies] = additionalDependency;
					numDependencies++;
					#if DEBUG_JOBS
					dependenciesDebug += "[additional dependency]";
					#endif
				}

				#if DEBUG_JOBS
				UnityEngine.Debug.Log(typeof(T) + " depends on " + dependenciesDebug);
				#endif

				JobHandle handle;
				if (numDependencies == 0) {
					handle = default;
				} else if (numDependencies == 1) {
					handle = dependencies[0];
				} else {
					//JobDependencyAnalyzerAssociated.combineSampler.Begin();
					handle = JobHandle.CombineDependencies(dependencies.Slice(0, numDependencies));
					//JobDependencyAnalyzerAssociated.combineSampler.End();
				}
				dependencies.Dispose();
				//JobDependencyAnalyzerAssociated.getDependenciesSampler.End();
				return handle;
			}
		}

		internal static void Scheduled (ref T data, JobDependencyTracker tracker, JobHandle job) {
			unsafe {
				int jobHash = JobDependencyAnalyzerAssociated.jobCounter++;
				// Note: data is a struct. It is stored on the stack and can thus not be moved by the GC.
				// Therefore we do not need to pin it first.
				// It is guaranteed to be stored on the stack since the Schedule method takes the data parameter by value and not by reference.
				byte* dataPtr = (byte*)UnsafeUtility.AddressOf(ref data);
				for (int i = 0; i < reflectionData.fieldOffsets.Length; i++) {
					// This is the internal value of the m_Buffer field of the NativeArray
					void* nativeArrayBufferPtr = *(void**)(dataPtr + reflectionData.fieldOffsets[i]);

					// Use the pointer as a hash to uniquely identify a NativeArray
					var hash = (long)nativeArrayBufferPtr;
					#if DEBUG_JOBS
					if (reflectionData.writes[i]) tracker.JobWritesTo(job, hash, jobHash, typeof(T).Name);
					else tracker.JobReadsFrom(job, hash, jobHash, typeof(T).Name);
					#else
					if (reflectionData.writes[i]) tracker.JobWritesTo(job, hash, jobHash);
					else tracker.JobReadsFrom(job, hash, jobHash);
					#endif
				}
			}
		}
	}
}
