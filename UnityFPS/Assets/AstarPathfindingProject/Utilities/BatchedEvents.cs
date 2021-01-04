using Unity.Mathematics;
using UnityEngine;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace Pathfinding.Util {
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_util_1_1_batched_events.php")]
	public class BatchedEvents : VersionedMonoBehaviour {
		const int ArchetypeOffset = 22;
		const int ArchetypeMask = 0xFF << ArchetypeOffset;

		static Archetype[] data = new Archetype[0];
		static BatchedEvents instance;
		static bool isIterating = false;

		[System.Flags]
		public enum Event {
			Update = 1 << 0,
			LateUpdate = 1 << 1,
			FixedUpdate = 1 << 2,
		};

		struct Archetype {
			public object[] objects;
			public int objectCount;
			public System.Type type;
			public int variant;
			public int archetypeIndex;
			public Event events;
			public System.Action<object[], int, Event> action;
			public CustomSampler sampler;

			public void Add (object obj) {
				objectCount++;
				UnityEngine.Assertions.Assert.IsTrue(objectCount < (1 << ArchetypeOffset));
				if (objects == null) objects = (object[])System.Array.CreateInstance(type, math.ceilpow2(objectCount));
				if (objectCount > objects.Length) {
					var newObjects = System.Array.CreateInstance(type, math.ceilpow2(objectCount));
					objects.CopyTo(newObjects, 0);
					objects = (object[])newObjects;
				}
				objects[objectCount-1] = obj;
				((IEntityIndex)obj).EntityIndex = (archetypeIndex << ArchetypeOffset) | (objectCount-1);
			}

			public void Remove (int index) {
				objectCount--;
				((IEntityIndex)objects[objectCount]).EntityIndex = (archetypeIndex << ArchetypeOffset) | index;
				((IEntityIndex)objects[index]).EntityIndex = 0;
				objects[index] = objects[objectCount];
				objects[objectCount] = null;
			}
		}

		static void CreateInstance () {
			// If scripts are recompiled the the static variable will be lost.
			// Some users recompile scripts in play mode and then reload the scene (https://forum.arongranberg.com/t/rts-game-pathfinding/6623/48?u=aron_granberg)
			// which makes this a requirement
			instance = FindObjectOfType<BatchedEvents>();
			if (instance == null) {
				var go = new UnityEngine.GameObject("Batch Helper");
				instance = go.AddComponent<BatchedEvents>();
				go.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
				DontDestroyOnLoad(go);
			}
		}

		public static void Remove<T>(T obj) where T : IEntityIndex {
			int index = obj.EntityIndex;

			if (index == 0) return;
			if (isIterating) throw new System.Exception("Cannot add or remove entities during an event (Update/LateUpdate/...) that this helper initiated");

			var archetypeIndex = ((index & ArchetypeMask) >> ArchetypeOffset) - 1;
			index &= ~ArchetypeMask;
			UnityEngine.Assertions.Assert.IsTrue(data[archetypeIndex].type == obj.GetType());
			data[archetypeIndex].Remove(index);
		}

		public static void Add<T>(T obj, Event eventTypes, System.Action<T[], int> action, int archetypeVariant = 0) where T : class, IEntityIndex {
			Add(obj, eventTypes, null, action, archetypeVariant);
		}

		public static void Add<T>(T obj, Event eventTypes, System.Action<T[], int, Event> action, int archetypeVariant = 0) where T : class, IEntityIndex {
			Add(obj, eventTypes, action, null, archetypeVariant);
		}

		static void Add<T>(T obj, Event eventTypes, System.Action<T[], int, Event> action1, System.Action<T[], int> action2, int archetypeVariant = 0) where T : class, IEntityIndex {
			if (obj.EntityIndex != 0) {
				throw new System.ArgumentException("This object is already registered. Call Remove before adding the object again.");
			}
			if (isIterating) throw new System.Exception("Cannot add or remove entities during an event (Update/LateUpdate/...) that this helper initiated");

			if (instance == null) CreateInstance();

			// Add in a hash of the event types
			archetypeVariant = (int)eventTypes * 12582917;


			var type = obj.GetType();
			for (int i = 0; i < data.Length; i++) {
				if (data[i].type == type && data[i].variant == archetypeVariant) {
					data[i].Add(obj);
					return;
				}
			}

			{
				Memory.Realloc(ref data, data.Length + 1);
				// A copy is made here so that these variables are captured by the lambdas below instead of the original action1/action2 parameters.
				// If this is not done then the C# JIT will allocate a lambda capture object every time this function is executed
				// instead of only when we need to create a new archetype. Doing that would create a lot more unnecessary garbage.
				var ac1 = action1;
				var ac2 = action2;
				System.Action<object[], int, Event> a1 = (objs, count, ev) => ac1((T[])objs, count, ev);
				System.Action<object[], int, Event> a2 = (objs, count, ev) => ac2((T[])objs, count);
				data[data.Length - 1] = new Archetype {
					type = type,
					events = eventTypes,
					variant = archetypeVariant,
					archetypeIndex = (data.Length - 1) + 1, // Note: offset by +1 to ensure that entity index = 0 is an invalid index
					action = ac1 != null ? a1 : a2,
					sampler = CustomSampler.Create(type.Name),
				};
				data[data.Length - 1].Add(obj);
			}
		}

		void DoEvent (Event eventType) {
			try {
				isIterating = true;
				for (int i = 0; i < data.Length; i++) {
					if (data[i].objectCount > 0 && (data[i].events & eventType) != 0) {
						try {
							data[i].sampler.Begin();
							data[i].action(data[i].objects, data[i].objectCount, eventType);
						} finally {
							data[i].sampler.End();
						}
					}
				}
			} finally {
				isIterating = false;
			}
		}

		void Update () {
			DoEvent(Event.Update);
		}

		void LateUpdate () {
			DoEvent(Event.LateUpdate);
		}

		void FixedUpdate () {
			DoEvent(Event.FixedUpdate);
		}
	}
}
