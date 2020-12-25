using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class SimpleCharacterController : MonoBehaviour
{
	public I3DAnimationController animController;
	public Rigidbody body;
	public FPSBulletController bulletPrefab;
	public Transform shootPoint;
	public float moveSpeed = 100;
	public bool canControl = false;
	public Vector3 velocity;
	// Start is called before the first frame update
	void Start()
	{
		StartCoroutine(DoMove());
	}

	// Update is called once per frame
	void Update()
	{
		velocity = body.velocity;
		if (canControl)
		{
			if (Input.GetKey(KeyCode.W)) SetMove(MoveDirectEnum.UP);
			if (Input.GetKey(KeyCode.S)) SetMove(MoveDirectEnum.DOWN);
			if (Input.GetKey(KeyCode.A)) SetMove(MoveDirectEnum.LEFT);
			if (Input.GetKey(KeyCode.D)) SetMove(MoveDirectEnum.RIGHT);
			if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.A))
				SetMove(MoveDirectEnum.IDLE);
		}
	}
	IEnumerator DoMove()
	{
		for (int i = 0; i < 100; i++)
		{

			MoveDirectEnum moveWay = GameExtensions.RandomEnumValue<MoveDirectEnum>();
			MoveDirectEnum turnWay = GameExtensions.RandomEnumValue<MoveDirectEnum>();
			LookAt(GameplayController.Instance.mainCharacter);
			moveWay = MoveDirectEnum.UP;
			SetMove(moveWay);

			//SetTurn(turnWay);
			yield return Yielders.Get(UnityEngine.Random.Range(0.5f, 2.0f));
			SetMove(MoveDirectEnum.IDLE);
			SetAttack();
			yield return Yielders.Get(UnityEngine.Random.Range(0.5f, 2.0f));
		}

	}
	public void LookAt(Transform target)
	{
		transform.LookAt(target);
	}
	public void SetTurn(MoveDirectEnum moveEnum)
	{
		switch (moveEnum)
		{
			case MoveDirectEnum.UP:
				transform.DORotate(new Vector3(0, 0, 0), 1).SetRelative(true);
				break;
			case MoveDirectEnum.DOWN:
				transform.DORotate(new Vector3(0, 180, 0), 1).SetRelative(true);
				break;
			case MoveDirectEnum.LEFT:
				transform.DORotate(new Vector3(0, 90, 0), 1).SetRelative(true);
				break;
			case MoveDirectEnum.RIGHT:
				transform.DORotate(new Vector3(0, -90, 0), 1).SetRelative(true);
				break;
			case MoveDirectEnum.IDLE:
				break;
			default:
				break;
		}
	}
	public void SetMove(MoveDirectEnum moveEnum)
	{
		switch (moveEnum)
		{
			case MoveDirectEnum.UP:
				body.velocity = transform.forward * moveSpeed;

				break;
			case MoveDirectEnum.DOWN:
				body.velocity = transform.forward * -moveSpeed;

				break;
			case MoveDirectEnum.LEFT:
				body.velocity = transform.right * -moveSpeed;

				break;
			case MoveDirectEnum.RIGHT:
				body.velocity =transform.right * moveSpeed;

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

