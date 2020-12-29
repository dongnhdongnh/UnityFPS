using UnityEngine;
using System.Collections;
using UnityEngine.UI;
//  ----------------------------------------------
//  Author:     CuongCT <caothecuong91@gmail.com> 
//  Copyright (c) 2016 OneSoft JSC
// ----------------------------------------------
public class PlaySoundOnClick : MonoBehaviour {
	[SerializeField]
	AudioClip sound;
	Button _button;
	public Button button
	{
		get
		{
			if (_button == null) _button = GetComponent<Button>();
			return _button;
		}
	}

	void Awake(){
		if (button != null)
			button.onClick.AddListener (()=>MusicManager.Instance.PlayOneShot(sound));
	}
}
