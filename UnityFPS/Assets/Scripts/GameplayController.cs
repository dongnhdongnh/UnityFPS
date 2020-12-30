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

	List<Transform> SpawnPoints;
	public Transform Container_Enemy;
	public ICharacter PrefabEnemy;

	public MapLoader mapLoader;
	public List<MapCubeEvent> mapCubeEvent = new List<MapCubeEvent>();
	// Start is called before the first frame update
	void Start()
	{
	//	mainCharacter.gameObject.SetActive(false);
		mapLoader.LoadMaps(new List<string> { "m1", "m2", "m3", "m4" },
		new List<Vector3> { Vector3.zero, new Vector3(0, 0, -16), new Vector3(-16, 0, -16), new Vector3(-16, 0, 0) },
	   () =>
{
	InitPlayer();

	SpawnPoints = new List<Transform>();
	foreach (MapCubeEvent mapEvent in mapCubeEvent)
	{
		if (mapEvent.cubeEventType.Equals(CubeEventType.EnemySpawnPoint))
		{
			SpawnPoints.Add(mapEvent.transform);
		}
	}
	if (SpawnPoints != null && SpawnPoints.Count > 0)
		StartCoroutine(SpawnEnemies());
	else
		Debug.LogError("Have no EnemySpawnPoint");


}
);

		//InitMap();
	}

	// Update is called once per frame
	void Update()
	{

	}
	public void InitMap(List<string> mapList)
	{

	}
	public void InitPlayer()
	{
		mainCharacter.InitHP(10);
		guiIngameController.SetPlayerHP(10);
		mainCharacter.gameObject.SetActive(true);
	}

	IEnumerator SpawnEnemies()
	{
		while (true)
		{
			Transform _spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Count)];
			ICharacter _e = SimplePool.Spawn(PrefabEnemy, _spawnPoint.position, Quaternion.identity);
			_e.transform.parent = Container_Enemy;
			yield return Yielders.Get(Random.Range(5, 10));
		}

	}
}
