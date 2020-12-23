using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class FPSGunController : MonoBehaviour
{
	public GameObject HandView, GunView, GunShootPoint;
	public FPSBulletController BulletPrefab;


	bool onScope = false;
	Vector3 GunStartPosition;


	private void Awake()
	{
		GunStartPosition = GunView.transform.localPosition;
		//	HandView.transform.localPosition = new Vector3(0, 0, 0);
		LoadGunView();
	}
	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Shoot();
		}
		if (Input.GetMouseButtonDown(1))
		{
			SwitchGunView(true);
		}
		if (Input.GetMouseButtonUp(1))
		{
			SwitchGunView(false);
		}
	}
	void LoadGunView()
	{
		if (onScope)
		{
			HandView.transform.DOKill();
			HandView.transform.DOLocalMove(new Vector3(-0.03f, -0.43f, 0.09f), 0.5f);
		}
		else
		{
			HandView.transform.DOKill();
			HandView.transform.DOLocalMove(new Vector3(0.6f, -0.6f, 0.4f), 0.5f);
		}
	}
	public void SwitchGunView(bool isOnScope)
	{
		if (onScope == isOnScope) return;
		onScope = isOnScope;
		LoadGunView();
	}
	public void Shoot()
	{
		GunView.transform.DOKill();
		GunView.transform.localPosition = GunStartPosition;
		GunView.transform.DOLocalMoveZ(0.1f, 0.2f).SetRelative(true).OnComplete(
		() =>
		{
			GunView.transform.localPosition = GunStartPosition;
		}
		);
		FPSBulletController _bullet = SimplePool.Spawn(BulletPrefab, GunShootPoint.transform.position, GunShootPoint.transform.rotation);
		//_bullet.Init(-GunView.transform.forward);
		_bullet.GetComponent<Rigidbody>().velocity =
							_bullet.transform.forward * 50;
	}
}
