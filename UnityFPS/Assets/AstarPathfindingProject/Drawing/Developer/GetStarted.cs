using UnityEngine;
// Important for the script to be able to find the Draw class
using Drawing;

public class GetStarted : MonoBehaviour {
	void Update () {
		// Draw a cylinder at the object's position with a height of 2 and a radius of 0.5
		Draw.WireCylinder(transform.position, Vector3.up, 2f, 0.5f);
	}
}
