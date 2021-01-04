using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Drawing;

[HelpURL("http://arongranberg.com/astar/docs/class_curve_editor.php")]
public class CurveEditor : MonoBehaviour {
	List<CurvePoint> curves = new List<CurvePoint>();
	public Camera cam;

	class CurvePoint {
		public Vector2 position, controlPoint0, controlPoint1;
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Mouse0)) {
			curves.Add(new CurvePoint {
				position = (Vector2)Input.mousePosition,
				controlPoint0 = Vector2.zero,
				controlPoint1 = Vector2.zero,
			});
		}

		if (curves.Count > 0 && Input.GetKey(KeyCode.Mouse0) && ((Vector2)Input.mousePosition - curves[curves.Count - 1].position).magnitude > 2*2) {
			var point = curves[curves.Count - 1];
			point.controlPoint1 = (Vector2)Input.mousePosition - point.position;
			point.controlPoint0 = -point.controlPoint1;
		}

		Render();
	}

	void Render () {
		using (Draw.InScreenSpace(cam)) {
			for (int i = 0; i < curves.Count; i++) {
				Draw.CircleXY((Vector3)curves[i].position, 2, Color.blue);
			}

			for (int i = 0; i < curves.Count - 1; i++) {
				var p0 = curves[i].position;
				var p1 = p0 + curves[i].controlPoint1;
				var p3 = curves[i+1].position;
				var p2 = p3 + curves[i+1].controlPoint0;
				Draw.Bezier((Vector3)p0, (Vector3)p1, (Vector3)p2, (Vector3)p3);
			}
		}
	}
}
