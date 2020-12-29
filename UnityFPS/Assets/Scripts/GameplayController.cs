using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayController : Singleton<GameplayController>
{
	public Transform mainCharacter;
	public EnemyHealthBar enemyHealthBarPrefab;
	public RectTransform healthPanelRect;
	public GameObject Effect_BulletHit, Effect_Blood, Effect_dead;
	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}
}
