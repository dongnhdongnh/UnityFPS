using UnityEngine;
using Drawing;

public class GetStartedGizmos : MonoBehaviourGizmos {
	public override void DrawGizmos () {
		using (Draw.InLocalSpace(transform)) {
			// Draw a cylinder at the object's position with a height of 2 and a radius of 0.5
			Draw.WireCylinder(Vector3.zero, Vector3.up, 2f, 0.5f);
		}
	}
}
