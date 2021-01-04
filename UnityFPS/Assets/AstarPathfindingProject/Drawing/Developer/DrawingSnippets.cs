using UnityEngine;
using System.Collections;
using Drawing;

public class DrawingSnippets1 {
	/** [Draw.WithColor] */
	void Update () {
		using (Draw.WithColor(Color.red)) {
			Draw.Line(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
			Draw.Line(new Vector3(0, 0, 0), new Vector3(0, 1, 2));
		}
	}
	/** [Draw.WithColor] */
}

public class DrawingSnippets2 {
	/** [Draw.WithDuration] */
	void Update () {
		using (Draw.WithDuration(1.0f)) {
			Draw.Line(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
			Draw.Line(new Vector3(0, 0, 0), new Vector3(0, 1, 2));
		}
	}
	/** [Draw.WithDuration] */
}

public class DrawingSnippets3 {
	public Transform transform;

	/** [Draw.InLocalSpace] */
	void Update () {
		using (Draw.InLocalSpace(transform)) {
			// Draw a box at (0,0,0) relative to the current object
			// This means it will show up at the object's position
			// The box is also rotated and scaled with the transform
			Draw.WireBox(Vector3.zero, Vector3.one);
		}
	}
	/** [Draw.InLocalSpace] */
}

public class DrawingSnippets4 {
	/** [Draw.Line] */
	void Update () {
		Draw.Line(Vector3.zero, Vector3.up);
	}
	/** [Draw.Line] */

	void Misc () {
		/** [Draw.Ray1] */
		Draw.Ray(Vector3.zero, Vector3.up);
		/** [Draw.Ray1] */

		/** [Draw.Ray2] */
		Draw.Ray(Camera.main.ScreenPointToRay(Vector3.zero), 10);
		/** [Draw.Ray2] */

		/** [Draw.WireCylinder1] */
		// Draw a tilted cylinder between the points (0,0,0) and (1,1,1) with a radius of 0.5
		Draw.WireCylinder(Vector3.zero, Vector3.one, 0.5f, Color.magenta);
		/** [Draw.WireCylinder1] */

		/** [Draw.WireCylinder2] */
		// Draw a two meter tall cylinder at the world origin with a radius of 0.5
		Draw.WireCylinder(Vector3.zero, Vector3.up, 2, 0.5f, Color.magenta);
		/** [Draw.WireCylinder2] */

		/** [Draw.WireCapsule1] */
		// Draw a tilted capsule between the points (0,0,0) and (1,1,1) with a radius of 0.5
		Draw.WireCapsule(Vector3.zero, Vector3.one, 0.5f, Color.magenta);
		/** [Draw.WireCapsule1] */

		/** [Draw.WireSphere1] */
		// Draw a wire sphere at the origin with a radius of 0.5
		Draw.WireSphere(Vector3.zero, 0.5f, Color.magenta);
		/** [Draw.WireSphere1] */

		/** [Draw.Polyline] */
		// Draw a square
		Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
		/** [Draw.Polyline] */
	}
}


public class GetStartedSnippets : MonoBehaviourGizmos {
	void Update () {
		/** [GetStarted.Color1] */
		Draw.WireCylinder(transform.position, Vector3.up, 2f, 0.5f, Color.red);
		/** [GetStarted.Color1] */
	}

	void Scopes1 () {
		/** [GetStarted.Scopes1] */
		// Draw three red cubes
		using (Draw.WithColor(Color.red)) {
			Draw.WireBox(transform.position, Vector3.one);
			Draw.WireBox(transform.position + Vector3.right, Vector3.one);
			Draw.WireBox(transform.position - Vector3.right, Vector3.one);
		}
		/** [GetStarted.Scopes1] */
	}

	void Scopes2 () {
		/** [GetStarted.Scopes2] */
		using (Draw.InLocalSpace(transform)) {
			// Draw a box at (0,0,0) relative to the current object
			// This means it will show up at the object's position
			Draw.WireBox(Vector3.zero, Vector3.one);
		}

		// Equivalent code using the lower level WithMatrix scope
		using (Draw.WithMatrix(transform.localToWorldMatrix)) {
			Draw.WireBox(Vector3.zero, Vector3.one);
		}
		/** [GetStarted.Scopes2] */
	}

	/** [GizmoContext] */
	public override void DrawGizmos () {
		using (Draw.InLocalSpace(transform)) {
			if (GizmoContext.InSelection(this)) {
				// Draw a cylinder
				Draw.WireCylinder(Vector3.zero, Vector3.up, 2f, 0.5f, new Color(1, 0, 0, 1));
			} else {
				// Just draw a red circle with some transparency
				Draw.CircleXZ(Vector3.zero, 0.5f, new Color(1, 0, 0, 0.5f));
			}
		}
	}
	/** [GizmoContext] */
}
