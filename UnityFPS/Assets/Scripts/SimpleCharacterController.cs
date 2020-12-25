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
	public int HP;
	public float moveSpeed = 100;
	public bool canControl = false;
	public Vector3 velocity;

	int _currentHP = 0;
	float _currentStundTime = 0;
	// Start is called before the first frame update
	void Start()
	{
		StartCoroutine(DoMove());
		_currentHP = HP;
	}

	// Update is called once per frame
	void Update()
	{
		if (_currentStundTime > 0) _currentStundTime -= Time.deltaTime;
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
			SetAttack();

			yield return Yielders.Get(UnityEngine.Random.Range(0.5f, 2.0f));
			SetMove(MoveDirectEnum.IDLE);

			yield return Yielders.Get(UnityEngine.Random.Range(0.5f, 2.0f));
		}
	}
	public void SetHit()
	{
		animController.SetHit(_currentHP);
	}
	public void SetJump()
	{
		if (_currentStundTime > 0) return;
		Vector3 _forward = transform.forward;
		_forward.y = 10;
		body.AddForce(_forward * moveSpeed);
	}
	public void LookAt(Transform target)
	{
		transform.LookAt(target);
	}
	public void SetTurn(MoveDirectEnum moveEnum)
	{
		if (_currentStundTime > 0) return;
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
				body.velocity = transform.right * moveSpeed;

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
		if (_currentStundTime > 0) return;
		FPSBulletController _bullet = SimplePool.Spawn(bulletPrefab, shootPoint.position, shootPoint.rotation);
		_bullet.Init(shootPoint);
		animController.SetAttack();
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.CompareTag("Bullet"))
		{
			_currentHP--;
			_currentStundTime = 3;
			SetHit();
		}
	}
}
public enum MoveDirectEnum
{
	UP, DOWN, LEFT, RIGHT, IDLE
}

