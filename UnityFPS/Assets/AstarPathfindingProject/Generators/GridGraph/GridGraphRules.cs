using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.Serialization;
	using Pathfinding.Jobs;
	using Unity.Jobs;
	using Unity.Collections;
	using Unity.Burst;

	public class CustomGridGraphRuleEditorAttribute : System.Attribute {
		public System.Type type;
		public string name;
		public CustomGridGraphRuleEditorAttribute(System.Type type, string name) {
			this.type = type;
			this.name = name;
		}
	}

	/// <summary>
	/// Container for all rules in a grid graph.
	///
	/// <code>
	/// // Get the first grid graph in the scene
	/// var gridGraph = AstarPath.active.data.gridGraph;
	///
	/// gridGraph.rules.rules.Add(new RuleAnglePenalty {
	///     penaltyScale = 10000,
	///     curve = AnimationCurve.Linear(0, 0, 90, 1),
	/// });
	/// </code>
	///
	/// See: <see cref="Pathfinding.GridGraph.rules"/>
	/// See: grid-rules (view in online documentation for working links)
	/// </summary>
	[JsonOptIn]
	public class GridGraphRules {
		List<System.Action<Context> >[] callbacks;

		/// <summary>List of all rules</summary>
		[JsonMember]
		public List<GridGraphRule> rules = new List<GridGraphRule>();

		long lastHash;

		/// <summary>Context for when scanning or updating a graph</summary>
		public class Context {
			/// <summary>Graph which is being scanned or updated</summary>
			public GridGraph graph;
			/// <summary>Data for all the nodes as NativeArrays</summary>
			public GridGraph.GridGraphScanData data;
			/// <summary>Tracks job dependencies. Use when scheduling jobs.</summary>
			public JobDependencyTracker tracker;
		}

		long Hash () {
			long hash = 196613;

			for (int i = 0; i < rules.Count; i++) {
				if (rules[i] != null && rules[i].enabled) hash = hash * 1572869 ^ (long)rules[i].Hash;
			}
			return hash;
		}

		public void RebuildIfNecessary () {
			var newHash = Hash();

			if (newHash == lastHash && callbacks != null) return;
			lastHash = newHash;
			Rebuild();
		}

		public void Rebuild () {
			rules = rules ?? new List<GridGraphRule>();
			callbacks = callbacks ?? new List<System.Action<Context> >[5];
			for (int i = 0; i < callbacks.Length; i++) {
				if (callbacks[i] != null) callbacks[i].Clear();
			}
			for (int i = 0; i < rules.Count; i++) {
				if (rules[i].enabled) rules[i].Register(this);
			}
		}

		public void DisposeUnmanagedData () {
			if (rules != null) {
				for (int i = 0; i < rules.Count; i++) {
					if (rules[i] != null) {
						rules[i].DisposeUnmanagedData();
						rules[i].SetDirty();
					}
				}
			}
		}

		public bool ExecuteRule (GridGraphRule.Pass rule, Context context) {
			if (callbacks == null) Rebuild();
			var actions = callbacks[(int)rule];
			if (actions != null) {
				try {
					for (int i = 0; i < actions.Count; i++) actions[i](context);
				} catch (System.Exception e) {
					UnityEngine.Debug.LogException(e);
					return false;
				}
				return true;
			}
			return false;
		}

		public void Add (GridGraphRule.Pass rule, System.Action<Context> action) {
			var index = (int)rule;

			if (callbacks[index] == null) {
				callbacks[index] = new List<System.Action<Context> >();
			}
			callbacks[index].Add(action);
		}

		/// <summary>
		/// <code>
		///         struct Filter : GridGraphRules.IConnectionFilter {
		///             public IntRect bounds;
		///             public bool IsValidConnection (int x, int z, int direction) {
		///                 x += bounds.xmin;
		///                 z += bounds.ymin;
		///                 var nx = x + GridGraph.neighbourXOffsets[direction];
		///                 var nz = z + GridGraph.neighbourZOffsets[direction];
		///                 // ... todo
		///                 return true;
		///             }
		///         }
		///         </code>
		/// </summary>
	}

	/// <summary>
	/// Custom rule for a grid graph.
	/// See: <see cref="Pathfinding.GridGraphRules"/>
	/// See: grid-rules (view in online documentation for working links)
	/// </summary>
	[JsonDynamicType]
	public abstract class GridGraphRule {
		/// <summary>Only enabled rules are executed</summary>
		[JsonMember]
		public bool enabled = true;
		int dirty;

		/// <summary>Where in the scanning process a rule will be executed</summary>
		public enum Pass {
			/// <summary>
			/// Before the collision testing phase but after height testing.
			/// This is very early. Most data is not valid by this point.
			///
			/// You can use this if you want to modify the node positions and still have it picked up by the collision testing code.
			/// </summary>
			BeforeCollision,
			/// <summary>
			/// Before connections are calculated.
			/// At this point height testing, collision testing has been done (if they are enabled).
			///
			/// This is the most common pass to use.
			/// If you are modifying walkability here then connections and erosion will be calculated properly.
			/// </summary>
			BeforeConnections,
			/// <summary>
			/// After connections are calculated.
			///
			/// If you are modifying connections directly you should that in this pass.
			///
			/// Note: If erosion is used then this pass will be executed twice. One time before erosion and one time after erosion
			/// when the connections are calculated again.
			/// </summary>
			AfterConnections,
			/// <summary>
			/// After everything else.
			/// This pass is executed after everything else is done.
			/// You should not modify walkability in this pass because then the node connections will not be up to date.
			/// </summary>
			PostProcess,
		}

		/// <summary>
		/// Hash of the settings for this rule.
		/// The <see cref="Register"/> method will be called again whenever the hash changes.
		/// If the hash does not change it is assumed that the <see cref="Register"/> method does not need to be called again.
		/// </summary>
		public virtual int Hash => dirty;

		/// <summary>
		/// Call if you have changed any setting of the rule.
		/// This will ensure that any cached data the rule uses is rebuilt.
		/// If you do not do this then any settings changes may not affect the graph when it is rescanned or updated.
		/// </summary>
		public virtual void SetDirty () {
			dirty++;
		}

		/// <summary>
		/// Called when the rule is removed or the graph is destroyed.
		/// Use this to e.g. clean up any NativeArrays that the rule uses.
		///
		/// Note: The rule should remain valid after this method has been called.
		/// However the <see cref="Register"/> method is guaranteed to be called before the rule is executed again.
		/// </summary>
		public virtual void DisposeUnmanagedData () {
		}

		/// <summary>Does preprocessing and adds callbacks to the <see cref="GridGraphRules"/> object</summary>
		public virtual void Register (GridGraphRules rules) {
		}

		public interface IConnectionFilter {
			bool IsValidConnection (int dataX, int dataZ, int direction);
		}

		public interface INodeModifier {
			void ModifyNode (int dataIndex, int dataX, int dataZ);
		}

		public static void ForEachNode<T>(IntRect bounds, ref T callback) where T : struct, INodeModifier {
			int depth = bounds.Height;
			int width = bounds.Width;

			int i = 0;

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++, i++) {
					callback.ModifyNode(i, x, z);
				}
			}
		}

		public static void FilterNodeConnections<T>(IntRect bounds, NativeArray<int> nodeConnections, ref T filter) where T : struct, IConnectionFilter {
			int depth = bounds.Height;
			int width = bounds.Width;

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					var conn = nodeConnections[z*width + x];
					for (int i = 0; i < 8; i++) {
						if ((conn & (1 << i)) != 0 && !filter.IsValidConnection(x, z, i)) {
							conn &= ~(1 << i);
						}
					}
					nodeConnections[z*width + x] = conn;
				}
			}
		}

		[BurstCompile]
		public struct JobNodeModifier<T> : IJob where T : INodeModifier {
			public T modifier;
			public IntRect bounds;

			public JobNodeModifier(GridGraphRules.Context context, T modifier) {
				this.modifier = modifier;
				bounds = context.data.bounds;
			}

			public void Execute () {
				int depth = bounds.Height;
				int width = bounds.Width;

				int i = 0;

				for (int z = 0; z < depth; z++) {
					for (int x = 0; x < width; x++, i++) {
						modifier.ModifyNode(i, x, z);
					}
				}
			}
		}

		[BurstCompile]
		public struct JobConnectionFilter<T> : IJob where T : IConnectionFilter {
			public NativeArray<int> nodeConnections;
			public T filter;
			public IntRect bounds;

			public JobConnectionFilter(GridGraphRules.Context context, T filter) {
				nodeConnections = context.data.nodeConnections;
				this.filter = filter;
				bounds = context.data.bounds;
			}

			public void Execute () {
				int depth = bounds.Height;
				int width = bounds.Width;

				for (int z = 0; z < depth; z++) {
					for (int x = 0; x < width; x++) {
						var conn = nodeConnections[z*width + x];
						for (int i = 0; i < 8; i++) {
							if ((conn & (1 << i)) != 0 && !filter.IsValidConnection(x, z, i)) {
								conn &= ~(1 << i);
							}
						}
						nodeConnections[z*width + x] = conn;
					}
				}
			}
		}
	}
}
