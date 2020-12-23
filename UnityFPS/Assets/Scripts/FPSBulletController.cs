using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSBulletController : MonoBehaviour
{
	public Rigidbody BulletBody;

	Vector3 Director;

	float killTime = 5;
	private void OnEnable()
	{
		killTime = 10;
	}
	public void Init(Vector3 director)
	{
		this.Director = director;
		BulletBody.AddForce(director * 100);
	}

	private void Update()
	{
		killTime -= Time.deltaTime;
		if (killTime < 0)
			SimplePool.Despawn(gameObject);
	}
	private void OnCollisionStay(Collision collision)
	{
		//	SimplePool.Despawn(gameObject);
	}

}
