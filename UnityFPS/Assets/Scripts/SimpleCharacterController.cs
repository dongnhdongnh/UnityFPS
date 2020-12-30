using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

public class SimpleCharacterController : ICharacter
{
	public I3DAnimationController animController;
	public Rigidbody body;
	public FPSBulletController bulletPrefab;
	public Transform shootPoint;
	public float moveSpeed = 100;
	public bool canControl = false;

	public NavMeshAgent AIController;

	//	public Vector3 velocity;

	EnemyHealthBar healthBar { get; set; }
	float _currentStundTime = 0, _currentInAttack = 0;


	#region UNITYFUNCTION
	private void OnEnable()
	{
		InitData();
	}
	// Start is called before the first frame update
	void Start()
	{

		//StartCoroutine(DoMove());

	}

	// Update is called once per frame
	void Update()
	{
		if (_currentStundTime > 0) _currentStundTime -= Time.deltaTime;
		if (_currentInAttack > 0) _currentInAttack -= Time.deltaTime;
		if (Vector3.Distance(GameplayController.Instance.mainCharacter.transform.position, transform.position) > 5)
			AIController.SetDestination(GameplayController.Instance.mainCharacter.transform.position);
		else
if (_currentInAttack <= 0)
			SetAttack();
		//velocity = body.velocity;
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

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.CompareTag("Bullet"))
		{
			FPSBulletController _bullet = collision.gameObject.GetComponent<FPSBulletController>();
			if (_bullet != null && _bullet.Parent != null && _bullet.Parent.characterType.Equals(CharacterType.PLAYER))
			{
				HPCurrent--;
				_currentStundTime = 3;
				SetHit();
			}
		}
	}
	#endregion
	public void InitData()
	{
		this.HPCurrent = HPMax;
		GeneratePlayerHealthBar();
	}
	public void GeneratePlayerHealthBar()
	{
		if (healthBar != null) return;
		healthBar = Instantiate(GameplayController.Instance.enemyHealthBarPrefab);
		healthBar.SetHealthBarData(this.transform, GameplayController.Instance.healthPanelRect);
		//healthBar.transform.SetParent(healthPanelRect, false);
	}


	IEnumerator DoMove()
	{
		for (int i = 0; i < 100; i++)
		{

			MoveDirectEnum moveWay = GameExtensions.RandomEnumValue<MoveDirectEnum>();
			MoveDirectEnum turnWay = GameExtensions.RandomEnumValue<MoveDirectEnum>();
			LookAt(GameplayController.Instance.mainCharacter.transform);
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
		animController.SetHit(HPCurrent);
		SimplePool.Spawn(GameplayController.Instance.Effect_Blood, transform.position, transform.rotation);
		if (healthBar != null)
			healthBar.OnHealthChanged((float)HPCurrent / (float)HPMax);
		if (HPCurrent <= 0)
		{
			SimplePool.Spawn(GameplayController.Instance.Effect_dead, transform.position, transform.rotation);
			//SimplePool.Despawn(healthBar.gameObject);
			healthBar.gameObject.SetActive(false);
			SimplePool.Despawn(gameObject);
		}
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
		if (_currentInAttack > 0) return;
		_currentInAttack = 2;
		LookAt(GameplayController.Instance.mainCharacter.transform);
		animController.SetAttack();
	}

	public void DoAttack()
	{
		FPSBulletController _bullet = SimplePool.Spawn(bulletPrefab, shootPoint.position, shootPoint.rotation);
		_bullet.Init(shootPoint, this);
	}




}

