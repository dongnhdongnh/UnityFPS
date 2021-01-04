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

	[Space]
	public bool mapLoadOnRuntime = false;
	public MapLoader mapLoader;
	public List<MapCubeEvent> mapCubeEvent = new List<MapCubeEvent>();
	// Start is called before the first frame update
	void Start()
	{

		if (!mapLoadOnRuntime)
		{
			mapCubeEvent = new List<MapCubeEvent>(GameObject.FindObjectsOfType<MapCubeEvent>());
			InitEnemies();
		}
		//===Run time Load===
		if (mapLoader == null || !mapLoadOnRuntime) return;
		mainCharacter.Body.isKinematic = true;
		mapLoader.LoadMaps(new List<string> { "m2", "m2", "m3", "m4" },
		new List<Vector3> { Vector3.zero, new Vector3(0, 0, -16), new Vector3(-16, 0, -16), new Vector3(-16, 0, 0) },
	   () =>
{
	InitPlayer();
	InitEnemies();

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
		mainCharacter.Body.isKinematic = false;
	}
	public void InitEnemies()
	{
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

	IEnumerator SpawnEnemies()
	{
		while (true)
		{
			Transform _spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Count)];
			ICharacter _e = SimplePool.Spawn(PrefabEnemy, _spawnPoint.position + new Vector3(0, 10, 0), Quaternion.identity);
			_e.transform.parent = Container_Enemy;
			yield return Yielders.Get(Random.Range(50, 100));
		}

	}
}
