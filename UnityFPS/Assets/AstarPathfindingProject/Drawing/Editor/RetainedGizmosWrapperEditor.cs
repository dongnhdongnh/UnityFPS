using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Drawing {
	[CustomEditor(typeof(RetainedGizmosWrapper))]
	public class RetainedGizmosWrapperEditor : Editor {
		// Use this for initialization
		void Start () {
		}

		// Update is called once per frame
		void Update () {
		}

		void OnSceneGUI () {
			Debug.Log("Scene GUI " + Camera.current.name);
		}
	}
}
