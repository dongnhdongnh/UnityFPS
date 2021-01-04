using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Pathfinding.Jobs.Grid {
	// Using BurstCompile to compile a Job with Burst
	// Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
	// but on the first schedule
	[BurstCompile]
	public struct JobPrepareGridRaycast : IJob {
		public Matrix4x4 graphToWorld;
		public IntRect bounds;
		public Vector3 raycastOffset;
		public Vector3 raycastDirection;
		public LayerMask raycastMask;

		[WriteOnly]
		public NativeArray<RaycastCommand> raycastCommands;

		public void Execute () {
			const int RaycastMaxHits = 1;
			int width = bounds.Width;
			float raycastLength = raycastDirection.magnitude;

			for (int z = 0; z < bounds.Height; z++) {
				int zw = z * width;
				for (int x = 0; x < width; x++) {
					int idx = zw + x;
					var pos = graphToWorld.MultiplyPoint3x4(new Vector3((bounds.xmin + x) + 0.5f, 0, (bounds.ymin + z) + 0.5f));
					raycastCommands[idx] = new RaycastCommand(pos + raycastOffset, raycastDirection, raycastLength, raycastMask, RaycastMaxHits);
				}
			}
		}
	}

	/// <summary>result[i] = neither hit1[i] nor hit2[i] hit anything</summary>
	[BurstCompile]
	public struct JobMergeRaycastCollisionHits : IJob {
		[ReadOnly]
		public NativeArray<RaycastHit> hit1;

		[ReadOnly]
		public NativeArray<RaycastHit> hit2;

		[WriteOnly]
		public NativeArray<bool> result;

		public void Execute () {
			for (int i = 0; i < hit1.Length; i++) {
				result[i] = hit1[i].normal == Vector3.zero && hit2[i].normal == Vector3.zero;
			}
		}
	}

	[BurstCompile]
	public struct JobPrepareRaycasts : IJob {
		public Vector3 direction;
		public Vector3 originOffset;
		public float distance;
		public LayerMask mask;

		[ReadOnly]
		public NativeArray<Vector3> origins;

		[WriteOnly]
		public NativeArray<RaycastCommand> raycastCommands;

		public void Execute () {
			for (int i = 0; i < raycastCommands.Length; i++) {
				raycastCommands[i] = new RaycastCommand(origins[i] + originOffset, direction, distance, mask, 1);
			}
		}
	}

	[BurstCompile]
	public struct JobNodePositions : IJob {
		public Matrix4x4 graphToWorld;
		public IntRect bounds;

		[WriteOnly]
		public NativeArray<Vector3> nodePositions;

		public void Execute () {
			for (int z = 0; z < bounds.Height; z++) {
				int zw = z * bounds.Width;
				for (int x = 0; x < bounds.Width; x++) {
					int idx = zw + x;
					nodePositions[idx] = graphToWorld.MultiplyPoint3x4(new Vector3((bounds.xmin + x) + 0.5f, 0, (bounds.ymin + z) + 0.5f));
				}
			}
		}
	}

	[BurstCompile(FloatMode = FloatMode.Fast)]
	public struct JobNodeWalkable : IJob {
		public bool useRaycastNormal;
		public float maxSlope;
		public Vector3 up;
		public bool unwalkableWhenNoGround;

		[ReadOnly]
		public NativeArray<float4> nodeNormals;

		[WriteOnly]
		public NativeArray<bool> nodeWalkable;

		bool DidHit (RaycastHit hit) {
			return hit.normal != Vector3.zero;
		}

		public void Execute () {
			// Cosinus of the max slope
			float cosMaxSlopeAngle = math.cos(math.radians(maxSlope));
			float4 upNative = new float4(up.x, up.y, up.z, 0);

			for (int i = 0; i < nodeNormals.Length; i++) {
				// walkable will be set to false if no ground was found (unless that setting has been disabled)
				// The normal will only be non-zero if something was hit.
				bool didHit = math.any(nodeNormals[i]);
				var walkable = didHit || !unwalkableWhenNoGround;

				// Check if the node is on a slope steeper than permitted
				if (walkable && useRaycastNormal) {
					if (didHit) {
						// Take the dot product to find out the cosine of the angle it has (faster than Vector3.Angle)
						float angle = math.dot(nodeNormals[i], upNative);

						// Check if the ground is flat enough to stand on
						if (angle < cosMaxSlopeAngle) {
							walkable = false;
						}
					}
				}

				nodeWalkable[i] = walkable;
			}
		}
	}

	/// <summary>
	/// Calculates the grid connections for a single node.
	/// Note that to ensure that connections are completely up to date after updating a node you
	/// have to calculate the connections for both the changed node and its neighbours.
	///
	/// In a layered grid graph, this will recalculate the connections for all nodes
	/// in the (x,z) cell (it may have multiple layers of nodes).
	///
	/// See: CalculateConnections(GridNodeBase)
	/// </summary>
	[BurstCompile]
	public struct JobErosion : IJob {
		public IntRect bounds;
		public IntRect writeMask;
		public NumNeighbours neighbours;
		public int erosion;

		[ReadOnly]
		public NativeArray<int> nodeConnections;

		[ReadOnly]
		public NativeArray<bool> nodeWalkable;

		[WriteOnly]
		public NativeArray<bool> outNodeWalkable;

		// Note: the first 3 connections are to nodes with a higher x or z coordinate
		// The last 3 connections are to nodes with a lower x or z coordinate
		// This is required for the grassfire transform to work properly
		// This is a permutation of GridGraph.hexagonNeighbourIndices
		static readonly int[] hexagonNeighbourIndices = { 1, 2, 5, 0, 3, 7 };

		public void Execute () {
			int width = bounds.Width;
			int depth = bounds.Height;

			Debug.Assert(width * depth == outNodeWalkable.Length);

			NativeArray<int> neighbourOffsets = new NativeArray<int>(8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			for (int i = 0; i < 8; i++) neighbourOffsets[i] = GridGraph.neighbourZOffsets[i] * width + GridGraph.neighbourXOffsets[i];

			var erosionDistances = new NativeArray<int>(outNodeWalkable.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

			if (neighbours == NumNeighbours.Six) {
				// Use the grassfire transform: https://en.wikipedia.org/wiki/Grassfire_transform extended to hexagonal graphs
				for (int z = 1; z < depth - 1; z++) {
					for (int x = 1; x < width - 1; x++) {
						int index = z * width + x;
						int v = int.MaxValue;
						for (int i = 3; i < 6; i++) {
							int connection = hexagonNeighbourIndices[i];
							if ((nodeConnections[index] & (1 << connection)) == 0) v = -1;
							else v = math.min(v, erosionDistances[index + neighbourOffsets[connection]]);
						}

						erosionDistances[index] = v + 1;
					}
				}

				for (int z = depth - 2; z > 0; z--) {
					for (int x = width - 2; x > 0; x--) {
						int index = z * width + x;
						int v = int.MaxValue;
						for (int i = 0; i < 3; i++) {
							int connection = hexagonNeighbourIndices[i];
							if ((nodeConnections[index] & (1 << connection)) == 0) v = -1;
							else v = math.min(v, erosionDistances[index + neighbourOffsets[connection]]);
						}

						erosionDistances[index] = math.min(erosionDistances[index], v + 1);
					}
				}
			} else {
				/* Index offset to get neighbour nodes. Added to a node's index to get a neighbour node index.
				 *
				 * \code
				 *         Z
				 *         |
				 *         |
				 *
				 *      6  2  5
				 *       \ | /
				 * --  3 - X - 1  ----- X
				 *       / | \
				 *      7  0  4
				 *
				 *         |
				 *         |
				 * \endcode
				 */
				const int DirectionDown = 0;
				const int DirectionRight = 1;
				const int DirectionUp = 2;
				const int DirectionLeft = 3;

				// Use the grassfire transform: https://en.wikipedia.org/wiki/Grassfire_transform
				for (int z = 1; z < depth - 1; z++) {
					for (int x = 1; x < width - 1; x++) {
						int index = z * width + x;
						var v1 = erosionDistances[index + neighbourOffsets[DirectionDown]];
						if ((nodeConnections[index] & (1 << DirectionDown)) == 0) v1 = -1;
						var v2 = erosionDistances[index + neighbourOffsets[DirectionLeft]];
						if ((nodeConnections[index] & (1 << DirectionLeft)) == 0) v2 = -1;
						erosionDistances[index] = math.min(v1, v2) + 1;
					}
				}


				for (int z = depth - 2; z > 0; z--) {
					for (int x = width - 2; x > 0; x--) {
						int index = z * width + x;
						var v1 = erosionDistances[index + neighbourOffsets[DirectionUp]];
						if ((nodeConnections[index] & (1 << DirectionUp)) == 0) v1 = -1;
						var v2 = erosionDistances[index + neighbourOffsets[DirectionRight]];
						if ((nodeConnections[index] & (1 << DirectionRight)) == 0) v2 = -1;
						erosionDistances[index] = math.min(erosionDistances[index], math.min(v1, v2) + 1);
					}
				}
			}

			var relativeWriteMask = writeMask.Offset(new Int2(-bounds.xmin, -bounds.ymin));
			for (int z = relativeWriteMask.ymin; z <= relativeWriteMask.ymax; z++) {
				for (int x = relativeWriteMask.xmin; x <= relativeWriteMask.xmax; x++) {
					int index = z * width + x;
					outNodeWalkable[index] = nodeWalkable[index] & (erosionDistances[index] >= erosion);
				}
			}
		}
	}

	/// <summary>
	/// Calculates the grid connections for a set of nodes.
	///
	/// This is a IJobParallelForBatch job. Calculating the connections in multiple threads is faster
	/// but due to hyperthreading (used on most intel processors) the individual threads will become slower.
	/// It is still worth it though.
	/// </summary>
	[BurstCompile(FloatMode = FloatMode.Fast)]
	public struct JobCalculateConnections : IJobParallelForBatched {
		public float maxStepHeight;
		/// <summary>Normalized up direction</summary>
		public Vector3 up;
		public IntRect bounds;
		public NumNeighbours neighbours;
		public bool use2D;
		public bool cutCorners;
		public bool maxStepUsesSlope;

		[ReadOnly]
		public NativeArray<bool> nodeWalkable;

		[ReadOnly]
		public NativeArray<float4> nodeNormals;

		[ReadOnly]
		public NativeArray<Vector3> nodePositions;

		/// <summary>All bitpacked node connections</summary>
		[WriteOnly]
		public NativeArray<int> nodeConnections;

		public bool allowBoundsChecks => false;

		/// <summary>
		/// Check if a connection to node B is valid.
		/// Node A is assumed to be walkable already
		/// </summary>
		bool IsValidConnection (float4 nodePosA, float4 nodeNormalA, int nodeB, float4 up) {
			if (!nodeWalkable[nodeB]) return false;

			float4 nodePosB = new float4(nodePositions[nodeB].x, nodePositions[nodeB].y, nodePositions[nodeB].z, 0);
			if (!maxStepUsesSlope) {
				// Check their differences along the Y coordinate (well, the up direction really. It is not necessarily the Y axis).
				return math.abs(math.dot(up, nodePosB - nodePosA)) <= maxStepHeight;
			} else {
				float4 v = nodePosB - nodePosA;
				float heightDifference = math.dot(v, up);

				// Check if the step is small enough.
				// This is a fast path for the common case.
				if (math.abs(heightDifference) <= maxStepHeight) return true;

				float4 v_flat = (v - heightDifference * up) * 0.5f;

				// Math!
				// Calculates the approximate offset along the up direction
				// that the ground will have moved at the midpoint between the
				// nodes compared to the nodes' center points.
				float NDotU = math.dot(nodeNormalA, up);
				float offsetA = -math.dot(nodeNormalA - NDotU * up, v_flat);

				float4 nodeNormalB = nodeNormals[nodeB];
				NDotU = math.dot(nodeNormalB, up);
				float offsetB = math.dot(nodeNormalB - NDotU * up, v_flat);

				// Check the height difference with slopes taken into account.
				// Note that since we also do the heightDifference check above we will ensure slope offsets do not increase the height difference.
				// If we allowed this then some connections might not be valid near the start of steep slopes.
				return math.abs(heightDifference + offsetB - offsetA) <= maxStepHeight;
			}
		}

		public void Execute (int start, int count) {
			if (maxStepHeight <= 0 || use2D) maxStepHeight = float.PositiveInfinity;

			float4 up = new float4(this.up.x, this.up.y, this.up.z, 0);

			int width = bounds.Width;
			int depth = bounds.Height;
			NativeArray<int> neighbourOffsets = new NativeArray<int>(8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			for (int i = 0; i < 8; i++) neighbourOffsets[i] = GridGraph.neighbourZOffsets[i] * width + GridGraph.neighbourXOffsets[i];

			int hexagonConnectionMask = 0;
			for (int i = 0; i < GridGraph.hexagonNeighbourIndices.Length; i++) hexagonConnectionMask |= 1 << GridGraph.hexagonNeighbourIndices[i];

			// The loop is parallelized over z coordinates
			for (int z = start; z < start + count; z++) {
				for (int x = 0; x < width; x++) {
					// Bitpacked connections
					// bit 0 is set if connection 0 is enabled
					// bit 1 is set if connection 1 is enabled etc.
					int conns = 0;
					int nodeIndex = z * width + x;
					float4 pos = new float4(nodePositions[nodeIndex].x, nodePositions[nodeIndex].y, nodePositions[nodeIndex].z, 0);
					float4 normal = nodeNormals[nodeIndex];

					if (nodeWalkable[nodeIndex]) {
						if (x != 0 && z != 0 && x != width - 1 && z != depth - 1) {
							// Inner part of the grid. We can skip bounds checking for these.
							for (int i = 0; i < 8; i++) {
								int neighbourIndex = nodeIndex + neighbourOffsets[i];
								if (IsValidConnection(pos, normal, neighbourIndex, up)) {
									// Enable connection i
									conns |= 1 << i;
								}
							}
						} else {
							// Border node. These require bounds checking
							for (int i = 0; i < 8; i++) {
								int nx = x + GridGraph.neighbourXOffsets[i];
								int nz = z + GridGraph.neighbourZOffsets[i];

								// Check if the new position is inside the grid
								if (nx >= 0 && nz >= 0 && nx < width && nz < depth) {
									int neighbourIndex = nodeIndex + neighbourOffsets[i];
									if (IsValidConnection(pos, normal, neighbourIndex, up)) {
										// Enable connection i
										conns |= 1 << i;
									}
								}
							}
						}
					}

					switch (neighbours) {
					case NumNeighbours.Four:
						// The first 4 bits are the axis aligned connections
						nodeConnections[nodeIndex] = conns & 0xF;
						break;
					case NumNeighbours.Eight:
						if (cutCorners) {
							int axisConns = conns & 0xF;
							// If at least one axis aligned connection
							// is adjacent to this diagonal, then we can add a connection.
							// Bitshifting is a lot faster than calling node.HasConnectionInDirection.
							// We need to check if connection i and i+1 are enabled
							// but i+1 may overflow 4 and in that case need to be wrapped around
							// (so 3+1 = 4 goes to 0). We do that by checking both connection i+1
							// and i+1-4 at the same time. Either i+1 or i+1-4 will be in the range
							// from 0 to 4 (exclusive)
							int diagConns = (axisConns | (axisConns >> 1 | axisConns << 3)) << 4;

							// Filter out diagonal connections that are invalid
							// This will also filter out some bits which may be set to true above bit 8
							diagConns &= conns;
							nodeConnections[nodeIndex] = axisConns | diagConns;
						} else {
							int axisConns = conns & 0xF;
							// If exactly 2 axis aligned connections is adjacent to this connection
							// then we can add the connection
							// We don't need to check if it is out of bounds because if both of
							// the other neighbours are inside the bounds this one must be too
							int diagConns = (axisConns & (axisConns >> 1 | axisConns << 3)) << 4;

							// Filter out diagonal connections that are invalid
							// This will also filter out some bits which may be set to true above bit 8
							diagConns &= conns;
							nodeConnections[nodeIndex] = axisConns | diagConns;
						}
						break;
					case NumNeighbours.Six:
						// Hexagon layout
						nodeConnections[nodeIndex] = conns & hexagonConnectionMask;
						break;
					}
				}
			}
		}
	}

	struct JobCheckCollisions : IJob {
		[ReadOnly]
		public NativeArray<Vector3> nodePositions;
		[WriteOnly]
		public NativeArray<bool> collisionResult;
		public GraphCollision collision;

		public void Execute () {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			// Replacing the atomic safety handle becase otherwise we will not be able to read/write from the arrays.
			// This is because other jobs are scheduled that also read/write from them. The JobDependencyTracker ensures
			// that we do not read/write at the same time, but the job system doesn't know that.
			var handle = AtomicSafetyHandle.Create();

			NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nodePositions, handle);
			NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref collisionResult, handle);
#endif

			for (int i = 0; i < nodePositions.Length; i++) {
				collisionResult[i] = collision.Check(nodePositions[i]);
			}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle.CheckDeallocateAndThrow(handle);
			AtomicSafetyHandle.Release(handle);
#endif
		}
	}

	public struct JobReadNodeData : IJobParallelForBatched {
		public System.Runtime.InteropServices.GCHandle nodesHandle;
		public uint graphIndex;

		/// <summary>(width, depth) of the array that the <see cref="nodesHandle"/> refers to</summary>
		public Int2 nodeArrayBounds;
		public IntRect dataBounds;

		[WriteOnly]
		public NativeArray<Vector3> nodePositions;

		[WriteOnly]
		public NativeArray<uint> nodePenalties;

		[WriteOnly]
		public NativeArray<int> nodeTags;

		[WriteOnly]
		public NativeArray<int> nodeConnections;

		[WriteOnly]
		public NativeArray<bool> nodeWalkableWithErosion;

		[WriteOnly]
		public NativeArray<bool> nodeWalkable;

		public bool allowBoundsChecks => false;

		public void Execute (int startIndex, int count) {
			// This is a managed type, we need to trick Unity to allow this inside of a job
			var nodes = (GridNode[])nodesHandle.Target;

			var width = dataBounds.Width;

			for (int z = 0; z < dataBounds.Height; z++) {
				int offset1 = z*width;
				int offset2 = (z + dataBounds.ymin)*nodeArrayBounds.x + dataBounds.xmin;
				for (int x = 0; x < width; x++) {
					var nodeIdx = offset2 + x;
					var node = nodes[nodeIdx];
					var dataIdx = offset1 + x;
					nodePositions[dataIdx] = (Vector3)node.position;
					nodePenalties[dataIdx] = node.Penalty;
					nodeTags[dataIdx] = (int)node.Tag;
					nodeConnections[dataIdx] = node.GetAllConnectionInternal();
					nodeWalkableWithErosion[dataIdx] = node.Walkable;
					nodeWalkable[dataIdx] = node.WalkableErosion;
				}
			}
		}
	}

	public struct JobAssignNodeData : IJobParallelForBatched {
		public System.Runtime.InteropServices.GCHandle nodesHandle;
		public uint graphIndex;

		/// <summary>(width, depth) of the array that the <see cref="nodesHandle"/> refers to</summary>
		public Int2 nodeArrayBounds;
		public IntRect dataBounds;
		public IntRect writeMask;

		[ReadOnly]
		public NativeArray<Vector3> nodePositions;

		[ReadOnly]
		public NativeArray<uint> nodePenalties;

		[ReadOnly]
		public NativeArray<int> nodeTags;

		[ReadOnly]
		public NativeArray<int> nodeConnections;

		[ReadOnly]
		public NativeArray<bool> nodeWalkableWithErosion;

		[ReadOnly]
		public NativeArray<bool> nodeWalkable;

		public bool allowBoundsChecks => false;

		public void Execute (int startIndex, int count) {
			// This is a managed type, we need to trick Unity to allow this inside of a job
			var nodes = (GridNode[])nodesHandle.Target;

			var relativeMask = new IntRect(writeMask.xmin - dataBounds.xmin, writeMask.ymin - dataBounds.ymin, writeMask.xmax - dataBounds.xmin, writeMask.ymax - dataBounds.ymin);

			// Determinstically convert the indices to rows. It is much easier to process a number of whole rows.
			var zstart = startIndex/dataBounds.Width;
			var zend = (startIndex+count)/dataBounds.Width;

			relativeMask.ymin = math.max(relativeMask.ymin, zstart);
			relativeMask.ymax = math.min(relativeMask.ymax, zend);

			for (int z = relativeMask.ymin; z <= relativeMask.ymax; z++) {
				var zw = z*dataBounds.Width;
				var zw2 = (dataBounds.ymin + z)*nodeArrayBounds.x;
				for (int x = relativeMask.xmin; x <= relativeMask.xmax; x++) {
					int i = zw + x;
					int nodeInGridIndex = zw2 + (dataBounds.xmin + x);
					var node = nodes[nodeInGridIndex];
					node.GraphIndex = graphIndex;
					node.NodeInGridIndex = nodeInGridIndex;
					node.position = (Int3)nodePositions[i];
					node.Penalty = nodePenalties[i];
					node.Tag = (uint)nodeTags[i];
					node.SetAllConnectionInternal(nodeConnections[i]);
					node.Walkable = nodeWalkableWithErosion[i];
					node.WalkableErosion = nodeWalkable[i];
				}
			}
		}
	}
}
