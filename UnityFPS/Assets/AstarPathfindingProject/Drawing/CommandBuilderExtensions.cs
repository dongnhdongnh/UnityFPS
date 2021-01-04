// This file is automatically generated by a script based on the CommandBuilder API.
// This file adds additional overloads to the CommandBuilder API with convenience parameters like colors and durations.
using Unity.Burst;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Drawing {
	public partial struct CommandBuilder {
		/// <summary>\copydoc Line(float3,float3)</summary>
		public void Line (float3 a, float3 b, Color color) {
			PushColor(color);
			Reserve<Color32, LineData>();
			Add(Command.Line | Command.PushColorInline);
			Add((Color32) color);
			Add(new LineData { a = a, b = b });
			PopColor();
		}

		/// <summary>\copydoc Ray(float3,float3)</summary>
		public void Ray (float3 origin, float3 direction, Color color) {
			Line(origin, origin + direction, color);
		}

		/// <summary>\copydoc Ray(Ray,float)</summary>
		public void Ray (Ray ray, float length, Color color) {
			Line(ray.origin, ray.origin + ray.direction * length, color);
		}

		/// <summary>\copydoc CircleXZ(float3,float,float,float)</summary>
		public void CircleXZ (float3 center, float radius, float startAngle, float endAngle, Color color) {
			PushColor(color);
			Reserve<Color32, CircleXZData>();
			Add(Command.CircleXZ | Command.PushColorInline);
			Add((Color32) color);
			Add(new CircleXZData { center = center, radius = radius, startAngle = startAngle, endAngle = endAngle });
			PopColor();
		}

		/// <summary>\copydoc CircleXZ(float3,float,float,float)</summary>
		public void CircleXZ (float3 center, float radius, Color color) {
			CircleXZ(center, radius, 0f, 2 * Mathf.PI, color);
		}

		/// <summary>\copydoc CircleXY(float3,float,float,float)</summary>
		public void CircleXY (float3 center, float radius, float startAngle, float endAngle, Color color) {
			PushColor(color);
			PushMatrix(XZtoXYPlaneMatrix);
			CircleXZ(new float3(center.x, -center.z, center.y), radius, startAngle, endAngle);
			PopMatrix();
			PopColor();
		}

		/// <summary>\copydoc CircleXY(float3,float,float,float)</summary>
		public void CircleXY (float3 center, float radius, Color color) {
			CircleXY(center, radius, 0f, 2 * Mathf.PI, color);
		}

		/// <summary>\copydoc Circle(float3,float3,float)</summary>
		public void Circle (float3 center, float3 normal, float radius, Color color) {
			PushColor(color);
			Reserve<Color32, CircleData>();
			Add(Command.Circle | Command.PushColorInline);
			Add((Color32) color);
			Add(new CircleData { center = center, normal = normal, radius = radius });
			PopColor();
		}

		/// <summary>\copydoc WireCylinder(float3,float3,float)</summary>
		public void WireCylinder (float3 bottom, float3 top, float radius, Color color) {
			WireCylinder(bottom, top - bottom, math.length(top - bottom), radius, color);
		}

		/// <summary>\copydoc WireCylinder(float3,float3,float,float)</summary>
		public void WireCylinder (float3 position, float3 up, float height, float radius, Color color) {
			PushColor(color);
			var tangent = math.normalizesafe(math.cross(up, new float3(1, 1, 1)));

			using (WithMatrix(Matrix4x4.TRS(position, Quaternion.LookRotation(tangent, up), new Vector3(radius, height, radius)))) {
				CircleXZ(float3.zero, 1);
				if (height > 0) {
					CircleXZ(new float3(0, 1, 0), 1);
					Line(new float3(1, 0, 0), new float3(1, 1, 0));
					Line(new float3(-1, 0, 0), new float3(-1, 1, 0));
					Line(new float3(0, 0, 1), new float3(0, 1, 1));
					Line(new float3(0, 0, -1), new float3(0, 1, -1));
				}
			}
			PopColor();
		}

		/// <summary>\copydoc WireCapsule(float3,float3,float)</summary>
		public void WireCapsule (float3 bottom, float3 top, float radius, Color color) {
			WireCapsule(bottom, top - bottom, math.length(top - bottom), radius, color);
		}

		/// <summary>\copydoc WireCapsule(float3,float3,float,float)</summary>
		public void WireCapsule (float3 position, float3 up, float height, float radius, Color color) {
			PushColor(color);
			up = math.normalizesafe(up);
			// Note; second parameter is normalized (1,1,1)
			var tangent = math.cross(up, new float3(0.577350269f, 0.577350269f, 0.577350269f));
			height = math.max(height, radius*2);

			using (WithMatrix(Matrix4x4.TRS(position, Quaternion.LookRotation(tangent, up), Vector3.one))) {
				CircleXZ(new float3(0, radius, 0), radius);
				CircleXY(new float3(0, radius, 0), radius, Mathf.PI, 2 * Mathf.PI);
				PushMatrix(XZtoYZPlaneMatrix);
				CircleXZ(new float3(radius, 0, 0), radius, Mathf.PI*0.5f, Mathf.PI*1.5f);
				PopMatrix();
				if (height > 0) {
					var upperY = height - radius;
					var lowerY = radius;
					CircleXZ(new float3(0, upperY, 0), radius);
					CircleXY(new float3(0, upperY, 0), radius, 0, Mathf.PI);
					PushMatrix(XZtoYZPlaneMatrix);
					CircleXZ(new float3(upperY, 0, 0), radius, -Mathf.PI*0.5f, Mathf.PI*0.5f);
					PopMatrix();
					Line(new float3(radius, lowerY, 0), new float3(radius, upperY, 0));
					Line(new float3(-radius, lowerY, 0), new float3(-radius, upperY, 0));
					Line(new float3(0, lowerY, radius), new float3(0, upperY, radius));
					Line(new float3(0, lowerY, -radius), new float3(0, upperY, -radius));
				}
			}
			PopColor();
		}

		/// <summary>\copydoc WireSphere(float3,float)</summary>
		public void WireSphere (float3 position, float radius, Color color) {
			PushColor(color);
			Circle(position, new float3(1, 0, 0), radius);
			Circle(position, new float3(0, 1, 0), radius);
			Circle(position, new float3(0, 0, 1), radius);
			PopColor();
		}

		/// <summary>\copydoc Polyline(List<Vector3>,bool)</summary>
		[BurstDiscard]
		public void Polyline (List<Vector3> points, bool cycle, Color color) {
			PushColor(color);
			for (int i = 0; i < points.Count - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Count > 1) Line(points[points.Count - 1], points[0]);
			PopColor();
		}

		/// <summary>\copydoc Polyline(List<Vector3>,bool)</summary>
		[BurstDiscard]
		public void Polyline (List<Vector3> points, Color color) {
			Polyline(points, false, color);
		}

		/// <summary>\copydoc Polyline(Vector3[],bool)</summary>
		[BurstDiscard]
		public void Polyline (Vector3[] points, bool cycle, Color color) {
			PushColor(color);
			for (int i = 0; i < points.Length - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
			PopColor();
		}

		/// <summary>\copydoc Polyline(Vector3[],bool)</summary>
		[BurstDiscard]
		public void Polyline (Vector3[] points, Color color) {
			Polyline(points, false, color);
		}

		/// <summary>\copydoc Polyline(float3[],bool)</summary>
		[BurstDiscard]
		public void Polyline (float3[] points, bool cycle, Color color) {
			PushColor(color);
			for (int i = 0; i < points.Length - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
			PopColor();
		}

		/// <summary>\copydoc Polyline(float3[],bool)</summary>
		[BurstDiscard]
		public void Polyline (float3[] points, Color color) {
			Polyline(points, false, color);
		}

		/// <summary>\copydoc Polyline(NativeArray<float3>,bool)</summary>
		public void Polyline (NativeArray<float3> points, bool cycle, Color color) {
			PushColor(color);
			for (int i = 0; i < points.Length - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
			PopColor();
		}

		/// <summary>\copydoc Polyline(NativeArray<float3>,bool)</summary>
		public void Polyline (NativeArray<float3> points, Color color) {
			Polyline(points, false, color);
		}

		/// <summary>\copydoc WireBox(float3,float3)</summary>
		public void WireBox (float3 center, float3 size, Color color) {
			WireBox(new Bounds(center, size), color);
		}

		/// <summary>\copydoc WireBox(float3,Quaternion,float3)</summary>
		public void WireBox (float3 center, Quaternion rotation, float3 size, Color color) {
			PushColor(color);
			using (WithMatrix(Matrix4x4.TRS(center, rotation, size))) {
				WireBox(new Bounds(new Vector3(0.5f, 0.5f, 0.5f), Vector3.one));
			}
			PopColor();
		}

		/// <summary>\copydoc WireBox(Bounds)</summary>
		public void WireBox (Bounds bounds, Color color) {
			PushColor(color);
			var min = bounds.min;
			var max = bounds.max;

			Line(new float3(min.x, min.y, min.z), new float3(max.x, min.y, min.z));
			Line(new float3(max.x, min.y, min.z), new float3(max.x, min.y, max.z));
			Line(new float3(max.x, min.y, max.z), new float3(min.x, min.y, max.z));
			Line(new float3(min.x, min.y, max.z), new float3(min.x, min.y, min.z));

			Line(new float3(min.x, max.y, min.z), new float3(max.x, max.y, min.z));
			Line(new float3(max.x, max.y, min.z), new float3(max.x, max.y, max.z));
			Line(new float3(max.x, max.y, max.z), new float3(min.x, max.y, max.z));
			Line(new float3(min.x, max.y, max.z), new float3(min.x, max.y, min.z));

			Line(new float3(min.x, min.y, min.z), new float3(min.x, max.y, min.z));
			Line(new float3(max.x, min.y, min.z), new float3(max.x, max.y, min.z));
			Line(new float3(max.x, min.y, max.z), new float3(max.x, max.y, max.z));
			Line(new float3(min.x, min.y, max.z), new float3(min.x, max.y, max.z));
			PopColor();
		}

		/// <summary>\copydoc CrossXZ(float3,float)</summary>
		public void CrossXZ (float3 position, float size, Color color) {
			PushColor(color);
			size *= 0.5f;
			Line(position - new float3(size, 0, 0), position + new float3(size, 0, 0));
			Line(position - new float3(0, 0, size), position + new float3(0, 0, size));
			PopColor();
		}

		/// <summary>\copydoc CrossXZ(float3,float)</summary>
		public void CrossXZ (float3 position, Color color) {
			CrossXZ(position, 1, color);
		}

		/// <summary>\copydoc CrossXY(float3,float)</summary>
		public void CrossXY (float3 position, float size, Color color) {
			PushColor(color);
			size *= 0.5f;
			Line(position - new float3(size, 0, 0), position + new float3(size, 0, 0));
			Line(position - new float3(0, size, 0), position + new float3(0, size, 0));
			PopColor();
		}

		/// <summary>\copydoc CrossXY(float3,float)</summary>
		public void CrossXY (float3 position, Color color) {
			CrossXY(position, 1, color);
		}

		/// <summary>\copydoc Bezier(float3,float3,float3,float3)</summary>
		public void Bezier (float3 p0, float3 p1, float3 p2, float3 p3, Color color) {
			PushColor(color);
			float3 prev = p0;

			for (int i = 1; i <= 20; i++) {
				float t = i/20.0f;
				float3 p = EvaluateCubicBezier(p0, p1, p2, p3, t);
				Line(prev, p);
				prev = p;
			}
			PopColor();
		}

		/// <summary>\copydoc SolidBox(float3,float3)</summary>
		public void SolidBox (float3 center, float3 size, Color color) {
			PushColor(color);
			Reserve<Color32, BoxData>();
			Add(Command.Box | Command.PushColorInline);
			Add((Color32) color);
			Add(new BoxData { center = center, size = size });
			PopColor();
		}

		/// <summary>\copydoc SolidBox(Bounds)</summary>
		public void SolidBox (Bounds bounds, Color color) {
			SolidBox(bounds.center, bounds.size, color);
		}

		/// <summary>\copydoc SolidBox(float3,Quaternion,float3)</summary>
		public void SolidBox (float3 center, Quaternion rotation, float3 size, Color color) {
			PushColor(color);
			using (WithMatrix(Matrix4x4.TRS(center, rotation, size))) {
				SolidBox(new Vector3(0.5f, 0.5f, 0.5f), Vector3.one);
			}
			PopColor();
		}
	}
}