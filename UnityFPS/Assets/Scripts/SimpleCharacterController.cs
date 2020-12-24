using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCharacterController : MonoBehaviour
{
	public I3DAnimationController animController;
	public Rigidbody body;
	public FPSBulletController bulletPrefab;
	public Transform shootPoint;
	public float moveSpeed = 100;
	// Start is called before the first frame update
	void Start()
	{
		StartCoroutine(DoMove());
	}

	// Update is called once per frame
	void Update()
	{

	}
	IEnumerator DoMove()
	{
		for (int i = 0; i < 100; i++)
		{

			MoveDirectEnum moveWay = GameExtensions.RandomEnumValue<MoveDirectEnum>();
			for (int j = 0; j < 3; i++)
			{
				yield return Yielders.Get(1);
				SetMove(moveWay);
			}
			SetAttack();
			yield return Yielders.Get(UnityEngine.Random.Range(2, 5));
		}

	}
	public void SetMove(MoveDirectEnum moveEnum)
	{
		switch (moveEnum)
		{
			case MoveDirectEnum.UP:
				body.velocity = new Vector3(0, 0, 1) * moveSpeed;
				break;
			case MoveDirectEnum.DOWN:
				body.velocity = new Vector3(0, 0, -1) * moveSpeed;
				break;
			case MoveDirectEnum.LEFT:
				body.velocity = new Vector3(-1, 0, 0) * moveSpeed;
				break;
			case MoveDirectEnum.RIGHT:
				body.velocity = new Vector3(1, 0, 0) * moveSpeed;
				break;
			case MoveDirectEnum.IDLE:
				body.velocity = Vector3.zero;
				break;
			default:
				break;
		}
		animController.SetMove(moveEnum);
	}
	public void SetAttack()
	{
		FPSBulletController _bullet = SimplePool.Spawn(bulletPrefab, shootPoint.position, shootPoint.rotation);
		_bullet.Init(shootPoint);
		animController.SetAttack();
	}
}
public enum MoveDirectEnum
{
	UP, DOWN, LEFT, RIGHT, IDLE
}

