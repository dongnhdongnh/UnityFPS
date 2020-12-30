using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
	public uteMapLoader mapLoader;
	int _mapIndex = 0;
	Vector3 _currentMapPosition;
	List<string> mapList;
	List<Vector3> mapPosition;
	bool isLoading = false;
	GameUtils.VoidEvent OnLoadMapDoneEvent;
	public bool isLoadMapDone = false;
	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		if (isLoading && mapLoader.isMapLoaded)
		{
			startLoadMap();
		}
	}

	public void LoadMaps(List<string> mapListInput, List<Vector3> mapPositionInput, GameUtils.VoidEvent OnLoadMapDone)
	{
		this.OnLoadMapDoneEvent = OnLoadMapDone;
		isLoadMapDone = false;
		mapList = mapListInput;
		mapPosition = mapPositionInput;
		_mapIndex = -1;
		startLoadMap();
		isLoading = true;

	}
	void startLoadMap()
	{
		_mapIndex++;
		if (_mapIndex >= mapList.Count)
		{
			isLoading = false;
			isLoadMapDone = true;
			if (this.OnLoadMapDoneEvent != null)
				this.OnLoadMapDoneEvent();
			return;
		}
		string _currentMap = mapList[_mapIndex];
		_currentMapPosition = mapPosition[_mapIndex];
		Debug.LogError("load map " + _currentMap);
		mapLoader.mapName = _currentMap;
		mapLoader.MapOffset = _currentMapPosition;
		mapLoader.LoadMapAsyncFromPoint(_currentMapPosition, 1);
	}
}
