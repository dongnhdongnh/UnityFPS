using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
	#region PRIVATE_VARIABLES
	private Vector2 positionCorrection = new Vector2(0, 100);
	float _showTime = 0.0f;
	#endregion
	#region PUBLIC_REFERENCES
	public RectTransform targetCanvas { get; set; }
	public RectTransform healthBar { get; set; }
	public Image fillBar;
	public Transform objectToFollow { get; set; }
	#endregion
	#region PUBLIC_METHODS
	public void SetHealthBarData(Transform targetTransform, RectTransform healthBarPanel)
	{
		this.targetCanvas = healthBarPanel;
		healthBar = GetComponent<RectTransform>();
		objectToFollow = targetTransform;
		RepositionHealthBar();
		//healthBar.gameObject.SetActive(true);
		this.transform.SetParent(this.targetCanvas, false);
	}
	public void OnHealthChanged(float healthFill)
	{
		fillBar.fillAmount = healthFill;
		_showTime = 0.5f;
		gameObject.SetActive(true);
	}
	#endregion
	#region UNITY_CALLBACKS
	void Update()
	{
		RepositionHealthBar();
		if (_showTime > 0)
		{
			_showTime -= Time.deltaTime;
			if (_showTime <= 0)
				gameObject.SetActive(false);
		}
	}
	#endregion
	#region PRIVATE_METHODS
	private void RepositionHealthBar()
	{
		Vector2 ViewportPosition = Camera.main.WorldToViewportPoint(objectToFollow.position);
		Vector2 WorldObject_ScreenPosition = new Vector2(
		((ViewportPosition.x * targetCanvas.sizeDelta.x) - (targetCanvas.sizeDelta.x * 0.5f)),
		((ViewportPosition.y * targetCanvas.sizeDelta.y) - (targetCanvas.sizeDelta.y * 0.5f)));
		//now you can set the position of the ui element
		healthBar.anchoredPosition = WorldObject_ScreenPosition;
	}
	#endregion
}
