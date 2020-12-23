using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class TextMeshProAddon : MonoBehaviour
{
	[SerializeField]
	TextMeshPro text;
	//float disableTime = 0;
	bool isIni = false;
	private void Awake()
	{
		if (text != null)
			text = GetComponent<TextMeshPro>();
	}
	public void Init(string text, float despawnTime)
	{
		this.text.text = text;
		// disableTime = despawnTime;
		_temp_disTime = despawnTime;
		isIni = true;
	}
	float _temp_disTime = 0;

	private void OnEnable()
	{
		_temp_disTime = float.NegativeInfinity;
	}

	private void OnDisable()
	{
		if (isIni)
		{
			SimplePool.Despawn(gameObject, false);
		}
		isIni = false;
	}
	//private void OnDisable()
	//{
	//    //if(_temp_disTime)

	//    if (_temp_disTime > 0)
	//    {
	//        SimplePool.Despawn(gameObject);
	//    }
	//    //   Debug.LogError("show text " + disableTime);

	//}
	private void Update()
	{
		if (!isIni) return;
		_temp_disTime -= Time.deltaTime;
		if (_temp_disTime <= 0)
		{
			SimplePool.Despawn(gameObject, false);
			isIni = false;
		}
	}
}
