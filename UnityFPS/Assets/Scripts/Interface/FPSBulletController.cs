﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSBulletController : MonoBehaviour
{
	Rigidbody _BulletBody;
	public Rigidbody BulletBody
	{
		get
		{
			if (_BulletBody == null)
				_BulletBody = GetComponent<Rigidbody>();
			return _BulletBody;
		}
	}

	Vector3 Director;

	float killTime = 5;
	private void OnEnable()
	{
		killTime = 10;
	}
	//public void Init(Vector3 director)
	//{
	//	this.Director = director;
	//	BulletBody.AddForce(director * 100);
	//}
	public void Init(Transform shootPoint)
	{
		transform.rotation = shootPoint.rotation;
		transform.position = shootPoint.position;
		BulletBody.velocity = transform.forward * 50;
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