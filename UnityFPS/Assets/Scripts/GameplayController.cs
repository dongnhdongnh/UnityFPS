using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayController : Singleton<GameplayController>
{
	public GUIInGameController guiIngameController;
	public FPSGunController mainCharacter;
	public EnemyHealthBar enemyHealthBarPrefab;
	public RectTransform healthPanelRect;
	public GameObject Effect_BulletHit, Effect_Blood, Effect_dead;
	// Start is called before the first frame update
	void Start()
	{
		InitPlayer();
	}

	// Update is called once per frame
	void Update()
	{

	}

	public void InitPlayer()
	{
		mainCharacter.InitHP(10);
		guiIngameController.SetPlayerHP(10);
	}
}
