using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIInGameController : Singleton<GUIInGameController>
{
	public GameObject TakeDamEffect;
	public Transform PanelPlayerHP;
	List<GameObject> _PlayerHPObject;
	List<GameObject> PlayerHPObject
	{
		get
		{
			if (_PlayerHPObject == null)
			{
				_PlayerHPObject = new List<GameObject>();
				foreach (Transform item in PanelPlayerHP)
				{
					_PlayerHPObject.Add(item.gameObject);
				}
			}
			return _PlayerHPObject;
		}
	}

	float _temp_takeDamEffect;


	public void SetPlayerHP(int value)
	{
		for (int i = PlayerHPObject.Count - 1; i >= 0; i--)
		{
			PlayerHPObject[i].SetActive(i <= value);
		}

	}
	public void SetTakeDameEffect()
	{
		_temp_takeDamEffect = 2;
		TakeDamEffect.SetActive(true);
	}
	private void Update()
	{
		if (_temp_takeDamEffect > 0)
		{
			_temp_takeDamEffect -= Time.deltaTime;
			if (_temp_takeDamEffect <= 0)
				TakeDamEffect.SetActive(false);
		}
	}
}
