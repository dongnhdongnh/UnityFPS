using UnityEngine;
using System.Collections;

public class PlayOnAwake : MonoBehaviour {
    public AudioClip audioClip;
	// Use this for initialization
	void OnEnable () {
        MusicManager.Instance.PlayOneShot(audioClip);
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
