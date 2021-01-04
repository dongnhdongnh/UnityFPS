namespace Pathfinding.Jobs {
	using UnityEngine;
	using Unity.Burst;
	using Unity.Collections;
	using Unity.Jobs;
	using Unity.Mathematics;
	using Unity.Collections.LowLevel.Unsafe;

	/// <summary>Helpers for scheduling simple NativeArray jobs</summary>
	static class NativeArrayExtensions {
		/// <summary>this[i] = value</summary>
		public static JobMemSet<T> MemSet<T>(this NativeArray<T> self, T value) where T : struct {
			return new JobMemSet<T> {
					   data = self,
					   value = value,
			};
		}

		/// <summary>this[i] &= other[i]</summary>
		public static JobAND BitwiseAndWith (this NativeArray<bool> self, NativeArray<bool> other) {
			return new JobAND {
					   result = self,
					   data = other,
			};
		}

		/// <summary>to[i] = from[i]</summary>
		public static JobCopy<T> CopyToJob<T>(this NativeArray<T> from, NativeArray<T> to) where T : struct {
			return new JobCopy<T> {
					   from = from,
					   to = to,
			};
		}

		public static JobCopyRectangleIntoLargerBuffer<T> CopyRectangleIntoLargerBuffer<T>(this NativeArray<T> input, NativeArray<T> output, Int2 outputSize, IntRect outputBounds) where T : struct {
			return new JobCopyRectangleIntoLargerBuffer<T> {
					   input = input,
					   output = output,
					   outputSize = outputSize,
					   outputBounds = outputBounds,
			};
		}
	}

	/// <summary>Treats input as a rectangle and copies it into the output at the specified position</summary>
	[BurstCompile]
	public struct JobCopyRectangleIntoLargerBuffer<T> : IJob where T : struct {
		[ReadOnly]
		[DisableUninitializedReadCheck] // TODO: Fix so that job doesn't run instead
		public NativeArray<T> input;

		[WriteOnly]
		public NativeArray<T> output;

		public Int2 outputSize;
		public IntRect outputBounds;

		public void Execute () {
			Copy(input, output, outputSize, outputBounds);
		}

		public static void Copy (NativeArray<T> input, NativeArray<T> output, Int2 outputSize, IntRect outputBounds) {
			UnityEngine.Assertions.Assert.IsTrue(input.Length == outputBounds.Width*outputBounds.Height);
			UnityEngine.Assertions.Assert.IsTrue(outputBounds.xmin >= 0 && outputBounds.ymin >= 0 && outputBounds.xmax <= outputSize.x && outputBounds.ymax <= outputSize.y);

			for (int z = 0; z < outputBounds.Height; z++) {
				var rowOffset = (z + outputBounds.ymin)*outputSize.x + outputBounds.xmin;
				for (int x = 0; x < outputBounds.Width; x++) {
					output[rowOffset + x] = input[z*outputBounds.Width + x];
				}
			}
		}
	}

	/// <summary>Treats input as a rectangle and copies it into the output at the specified position</summary>
	[BurstCompile]
	public struct JobCopyRectangle<T> : IJob where T : struct {
		[ReadOnly]
		[DisableUninitializedReadCheck] // TODO: Fix so that job doesn't run instead
		public NativeArray<T> input;

		[WriteOnly]
		public NativeArray<T> output;

		public Int2 inputSize;
		public Int2 outputSize;
		public IntRect inputBounds;
		public IntRect outputBounds;

		public void Execute () {
			Copy(input, output, inputSize, outputSize, inputBounds, outputBounds);
		}

		// Unity asserts don't work well inside Burst
		static void Assert (bool value) {
#if UNITY_ASSERTIONS
			if (!value) throw new System.Exception();
#endif
		}

		public static void Copy (NativeArray<T> input, NativeArray<T> output, Int2 inputSize, Int2 outputSize, IntRect inputBounds, IntRect outputBounds) {
			Assert(input.Length == inputSize.x*inputSize.y);
			Assert(output.Length == outputSize.x*outputSize.y);
			Assert(inputBounds.xmin >= 0 && inputBounds.ymin >= 0 && inputBounds.xmax < inputSize.x && inputBounds.ymax < inputSize.y);
			Assert(outputBounds.xmin >= 0 && outputBounds.ymin >= 0 && outputBounds.xmax < outputSize.x && outputBounds.ymax < outputSize.y);
			Assert(inputBounds.Width == outputBounds.Width && inputBounds.Height == outputBounds.Height);

			if (inputSize == outputSize && inputBounds.Width == inputSize.x && inputBounds.Height == inputSize.y) {
				// One contiguous chunk
				input.CopyTo(output);
			} else {
				// Copy row-by-row
				for (int z = 0; z < outputBounds.Height; z++) {
					var rowOffsetInput = (z + inputBounds.ymin)*inputSize.x + inputBounds.xmin;
					var rowOffsetOutput = (z + outputBounds.ymin)*outputSize.x + outputBounds.xmin;
					// Using a raw MemCpy call is a bit faster, but that requires unsafe code
					// Using a for loop is *a lot* slower (except for very small arrays, in which case it is about the same or very slightly faster).
					NativeArray<T>.Copy(input, rowOffsetInput, output, rowOffsetOutput, outputBounds.Width);
				}
			}
		}
	}

	/// <summary>result[i] = value</summary>
	[BurstCompile]
	public struct JobMemSet<T> : IJob where T : struct {
		[WriteOnly]
		public NativeArray<T> data;

		public T value;

		public void Execute () {
			// TODO: Use memset?
			for (int i = 0; i < data.Length; i++) {
				data[i] = value;
			}
		}
	}

	/// <summary>to[i] = from[i]</summary>
	[BurstCompile]
	public struct JobCopy<T> : IJob where T : struct {
		[ReadOnly]
		public NativeArray<T> from;

		[WriteOnly]
		public NativeArray<T> to;

		public void Execute () {
			from.CopyTo(to);
		}
	}

	/// <summary>result[i] &= data[i]</summary>
	[BurstCompile]
	public struct JobAND : IJob {
		public NativeArray<bool> result;

		[ReadOnly]
		public NativeArray<bool> data;

		public void Execute () {
			for (int i = 0; i < result.Length; i++) {
				result[i] &= data[i];
			}
		}
	}

	/// <summary>
	/// Copies hit points and normals.
	/// points[i] = hits[i].point (if anything was hit), normals[i] = hits[i].normal.normalized.
	///
	/// Warning: Deallocates hits
	/// </summary>
	[BurstCompile]
	public struct JobCopyHits : IJob {
		[ReadOnly]
		public NativeArray<RaycastHit> hits;

		[WriteOnly]
		public NativeArray<Vector3> points;

		[WriteOnly]
		public NativeArray<float4> normals;

		public void Execute () {
			for (int i = 0; i < hits.Length; i++) {
				var normal = hits[i].normal;
				normals[i] = math.normalizesafe(new float4(normal.x, normal.y, normal.z, 0));
				// Check if anything was hit. The normal will be zero otherwise
				// If nothing was hit then the existing data in the points array is reused
				if (math.any(normal)) {
					points[i] = hits[i].point;
				}
			}
		}
	}
}
