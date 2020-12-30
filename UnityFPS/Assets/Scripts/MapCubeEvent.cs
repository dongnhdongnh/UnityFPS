using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapCubeEvent : MonoBehaviour
{
	public CubeEventType cubeEventType;

	private void OnEnable()
	{
		if (GameplayController.Instance != null)
			GameplayController.Instance.mapCubeEvent.Add(this);
		else
			Debug.LogError("Have no gameplaycontroller");
	}
	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}
}

public enum CubeEventType
{
	EnemySpawnPoint, PlayerSpawnPoint
}
