using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class DOTTweenAddon : MonoBehaviour
{

	public DOTweenAnimation[] tweens;
	public bool replayOnEnable = true;
	private void OnEnable()
	{
		if (replayOnEnable)
		{
			Restart();
		}
	}

	public void Restart()
	{
		foreach (DOTweenAnimation item in tweens)
		{
			if (item != null)
				item.DORestart();
		}
	}
}
