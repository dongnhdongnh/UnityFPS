using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Drawing {
	public abstract class MonoBehaviourGizmos : MonoBehaviour, IDrawGizmosWithVersion {
		public int GizmoVersion { get; private set; }

		public MonoBehaviourGizmos() {
			RetainedGizmosWrapper.Register(this);
		}

		void OnDrawGizmos () {
			GizmoVersion = RetainedGizmosWrapper.gizmoVersion;
		}

		public virtual void DrawGizmos () {
		}
	}
}
