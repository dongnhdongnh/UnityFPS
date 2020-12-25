using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAnimationController : I3DAnimationController
{
	public Animator animator;
	public override void SetMove(MoveDirectEnum moveEnum)
	{
		base.SetMove(moveEnum);
		switch (moveEnum)
		{
			case MoveDirectEnum.UP:
				animator.SetInteger("move", 1);
				break;
			case MoveDirectEnum.DOWN:
				animator.SetInteger("move", -1);
				break;
			case MoveDirectEnum.LEFT:
				animator.SetInteger("move", -2);
				break;
			case MoveDirectEnum.RIGHT:
				animator.SetInteger("move", 2);
				break;
			case MoveDirectEnum.IDLE:
				animator.SetInteger("move", 0);
				break;
			default:
				break;
		}
	}

	public override void SetAttack()
	{
		base.SetAttack();
		animator.SetTrigger("attack");
	}
	public override void SetHit()
	{
		base.SetHit();
		animator.SetTrigger("gethit");
	}
}
