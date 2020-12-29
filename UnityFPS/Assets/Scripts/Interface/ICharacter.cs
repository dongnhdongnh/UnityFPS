using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ICharacter : MonoBehaviour
{
	public CharacterType characterType;
	public int HPMax;
	public int HPCurrent { get; set; }

	public void InitHP(int value)
	{
		HPMax = value;
		HPCurrent = HPMax;
	}
}
public enum MoveDirectEnum
{
	UP, DOWN, LEFT, RIGHT, IDLE
}
public enum CharacterType
{
	PLAYER, ENEMY
}
