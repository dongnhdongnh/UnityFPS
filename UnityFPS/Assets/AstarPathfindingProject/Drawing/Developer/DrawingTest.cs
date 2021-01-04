using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Drawing;
using Unity.Mathematics;

public class DrawingTest : MonoBehaviourGizmos {
	public Camera cam;

	RenderTexture rt1x;
	RenderTexture rt2x;

	const int width = 190;

	void SetCam2D () {
		cam.transform.position = new Vector3(0, 0, -2);
		cam.transform.eulerAngles = Vector3.zero;
	}

	void SetCamXZ () {
		cam.transform.position = new Vector3(0, 2, 0);
		cam.transform.eulerAngles = new Vector3(90, 0, 0);
	}

	void SetCam3D () {
		cam.transform.position = new Vector3(-1, 0.847f, -1.515f);
		cam.transform.eulerAngles = new Vector3(29.25f, 33.5f, 0);
	}

	// Start is called before the first frame update
	IEnumerator Start () {
		rt1x = new RenderTexture(width, width, 16, UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
		rt2x = new RenderTexture(width*2, width*2, 16, UnityEngine.Experimental.Rendering.DefaultFormat.LDR);

		var col = Color.black;
		SetCamXZ();
		Draw.CircleXZ(float3.zero, 1, col);
		Render("CircleXZ");
		yield return null;

		Draw.CircleXZ(float3.zero, 1, -Mathf.PI*0.25f, Mathf.PI*1.25f, col);
		Render("CircleXZ_segment");
		yield return null;

		Draw.CrossXZ(float3.zero, col);
		Render("CrossXZ");
		yield return null;

		const float size = 0.5f;
		Draw.Polyline(new List<Vector3> { new Vector3(-size*1.5f, 0, -size*0.5f), new Vector3(-size*0.5f, 0, size*0.5f), new Vector3(size*0.5f, 0, -size*0.5f), new Vector3(size*1.5f, 0, size*0.5f) }, col);
		Render("Polyline");
		yield return null;

		Draw.Line(new float3(-0.5f, 0, -0.5f), new float3(0.5f, 0, 0.5f), col);
		Render("Line");
		yield return null;

		const float bezierSize = 0.75f;
		Draw.Bezier(new Vector3(-1f, 0, 0) * bezierSize, new Vector3(-1f + 1f, 0, 0 + 1f) * bezierSize, new Vector3(1f - 1f, 0, 0 - 1f) * bezierSize, new Vector3(1f, 0, 0) * bezierSize, col);
		Render("Bezier");
		yield return null;

		SetCam3D();
		Draw.WireCapsule(new float3(0, -0.75f, 0), new float3(0, 0.75f, 0), 0.5f, col);
		Render("WireCapsule");
		yield return null;

		Draw.WireCylinder(new float3(0, -0.5f, 0), new float3(0, 0.5f, 0), 0.5f, col);
		Render("WireCylinder");
		yield return null;

		Draw.WireSphere(new float3(0, 0, 0), 0.5f, col);
		Render("WireSphere");
		yield return null;

		Draw.WireBox(new float3(0, 0, 0), new float3(1, 1, 1), col);
		Render("WireBox");
		yield return null;

		Draw.SolidBox(new float3(0, 0, 0), new float3(1, 1, 1), col);
		Render("SolidBox");
		yield return null;

		rt1x.Release();
		rt2x.Release();
	}

	// Update is called once per frame
	void Render (string name) {
		if (Application.isPlaying) {
			RetainedGizmosWrapper.allowRenderToRenderTextures = true;
			RetainedGizmosWrapper.drawToAllCameras = true;
			cam.targetTexture = rt1x;
			cam.Render();
			cam.targetTexture = rt2x;
			cam.Render();
			RetainedGizmosWrapper.allowRenderToRenderTextures = false;
			RetainedGizmosWrapper.drawToAllCameras = false;

			var tex = new Texture2D(rt1x.width, rt1x.height);
			RenderTexture.active = rt1x;
			tex.ReadPixels(new Rect(0, 0, rt1x.width, rt1x.height), 0, 0);
			tex.Apply();
			System.IO.File.WriteAllBytes("DrawingDocumentation/images/rendered/" + name.ToLowerInvariant() + "@1x.png", tex.EncodeToPNG());
			Object.DestroyImmediate(tex);

			tex = new Texture2D(rt2x.width, rt2x.height);
			RenderTexture.active = rt2x;
			tex.ReadPixels(new Rect(0, 0, rt2x.width, rt2x.height), 0, 0);
			tex.Apply();
			System.IO.File.WriteAllBytes("DrawingDocumentation/images/rendered/" + name.ToLowerInvariant() + "@2x.png", tex.EncodeToPNG());
			Object.DestroyImmediate(tex);
		}
	}

	IEnumerator gizmoCoroutine;
	public override void DrawGizmos () {
		if (Application.isPlaying) return;

		if (gizmoCoroutine == null) gizmoCoroutine = Start();
		if (!gizmoCoroutine.MoveNext()) gizmoCoroutine = null;
		//Draw.WireCube(new float3(0, 0, 0), new float3(1, 1, 1), Color.white);
	}
}
