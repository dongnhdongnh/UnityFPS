using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAddon : MonoBehaviour
{

	public bool AutoDelAfterParticleDone = true;
	public float DelTimeInput = -1;
	public bool ReplayOnEnable = false;
	public bool NotKill = false;
	[SerializeField]
	ParticleSystem particleSystem;

	float DelTime = 0;
	float _temp_delTime = 0;
	private void Awake()
	{
		if (particleSystem == null)
		{
			particleSystem = GetComponent<ParticleSystem>();

		}
		if (AutoDelAfterParticleDone)
		{
			DelTime = particleSystem.duration;
		}
	}
	private void OnEnable()
	{
		if (!AutoDelAfterParticleDone)
			_temp_delTime = DelTimeInput;
		else
			_temp_delTime = DelTime;
		if (ReplayOnEnable)
		{
			particleSystem.Play(true);
		}
	}

	private void OnDisable()
	{
		if (gameObject.activeSelf)
		{
			//Debug.Log(gameObject.name + " still active");
			//	this.transform.parent = null;
			if (!NotKill)
				SimplePool.Despawn(gameObject, false);
			else
				gameObject.SetActive(false);
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (_temp_delTime >= 0)
		{
			_temp_delTime -= Time.deltaTime;
		}
		else
		{
			if (!NotKill)
				SimplePool.Despawn(gameObject);
			else
				gameObject.SetActive(false);
		}
	}
}
