using UnityEngine;
using System.Collections;
//common sound and music
using System.Collections.Generic;
using UnityEngine.Audio;


public class MusicManager : MonoBehaviour
{
	[SerializeField]
	private AudioMixer mixer;
	public AudioSource effectSource;
	public AudioSource musicSource;
	public float lowPitchRange = 0.95f;              //The lowest a sound effect will be randomly pitched.
	public float highPitchRange = 1.05f;            //The highest a sound effect will be randomly pitched.
	public float delayTime = 0.5f;
	public const string MASTER_KEY = "MASTER_KEY";
	public const string MUSIC_KEY = "MUSIC_KEY";
	public const string SOUND_KEY = "SOUND_KEY";

	public MusicDB MusicDB;

	private static MusicManager _instance;
	public static MusicManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = GameObject.FindObjectOfType<MusicManager>();
				if (_instance == null)
				{
					Debug.LogError("Music Manager NULL");
					return null;
				}
				//Tell unity not to destroy this object when loading a new scene!
				DontDestroyOnLoad(_instance.gameObject);

			}
			return _instance;
		}
	}

	public float MasterVolume
	{
		get
		{
			return PlayerPrefs.GetFloat(MASTER_KEY, 1.0f);
		}
		set
		{
			SetMasterVolume(value);
		}
	}
	public float MusicVolume
	{
		get
		{
			return PlayerPrefs.GetFloat(MUSIC_KEY, 1.0f);
		}
		set
		{
			SetMusicVolume(value);
		}
	}
	public float SoundVolume
	{
		get
		{
			return PlayerPrefs.GetFloat(SOUND_KEY, 1.0f);
		}
		set
		{
			SetSoundVolume(value);
		}
	}

	void Awake()
	{


		if (_instance == null)
		{
			//If I am the first instance, make me the Singleton
			_instance = this;
			SetMasterVolume(MasterVolume);
			SetMusicVolume(MusicVolume);
			SetSoundVolume(SoundVolume);
			DontDestroyOnLoad(this);
		}
		else
		{
			//If a Singleton already exists and you find
			//another reference in scene, destroy it!
			if (this != _instance)
			{
				Destroy(this.gameObject);
			}
		}
	}
	public void ReloadVolume()
	{
		SetMasterVolume(MasterVolume);
		SetMusicVolume(MusicVolume);
		SetSoundVolume(SoundVolume);
	}

	//Used to play single sound clips.
	public void PlaySingle(AudioClip clip)
	{
		//Set the clip of our efxSource audio source to the clip passed in as a parameter.

		effectSource.clip = clip;

		//Play the clip.
		effectSource.Play();
	}


	//Used to play single sound clips.
	public void PlayOneShot(AudioClip clip)
	{
		//Play the clip.
		//effectSource.Play(clip);
		//if (soundVol > 0)
		//	AudioSource.PlayClipAtPoint(clip, Vector3.zero);
		effectSource.PlayOneShot(clip);
	}

	//RandomizeSfx chooses randomly between various audio clips and slightly changes their pitch.
	public void RandomizeSfx(params AudioClip[] clips)
	{
		//Generate a random number between 0 and the length of our array of clips passed in.
		int randomIndex = Random.Range(0, clips.Length);

		//Choose a random pitch to play back our clip at between our high and low pitch ranges.
		float randomPitch = Random.Range(lowPitchRange, highPitchRange);

		//Set the pitch of the audio source to the randomly chosen pitch.
		effectSource.pitch = randomPitch;

		//Set the clip to the clip at our randomly chosen index.
		effectSource.clip = clips[randomIndex];

		//Play the clip.
		effectSource.Play();
	}

	public void PlayMusic(AudioClip clip)
	{
		SetMasterVolume(MasterVolume);
		SetMusicVolume(MusicVolume);
		SetSoundVolume(SoundVolume);
		StopMusic();
		musicSource.clip = clip;
		musicSource.PlayDelayed(delayTime);
	}
	public void RandomizeMusic(params AudioClip[] clips)
	{
		StopMusic();
		//Generate a random number between 0 and the length of our array of clips passed in.
		int randomIndex = Random.Range(0, clips.Length);

		//Set the clip to the clip at our randomly chosen index.
		musicSource.clip = clips[randomIndex];

		//Play the clip.
		musicSource.PlayDelayed(delayTime);
	}
	public void StopMusic()
	{
		musicSource.Stop();
	}


	private void SetMasterVolume(float volume)
	{
		mixer.SetFloat("MasterVolume", volume);
		PlayerPrefsOnline.SetFloat(MASTER_KEY, volume);
	}
	private void SetMusicVolume(float volume)
	{
		mixer.SetFloat("MusicVolume", volume);
		PlayerPrefsOnline.SetFloat(MUSIC_KEY, volume);
	}
	float soundVol = 0;
	private void SetSoundVolume(float volume)
	{
		soundVol = volume;
		mixer.SetFloat("SoundVolume", volume);
		PlayerPrefsOnline.SetFloat(SOUND_KEY, volume);
	}

	public void SetSoundVolumeWithoutSave(float volume)
	{
		mixer.SetFloat("SoundVolume", volume);
	}
	public void SetMusicVolumeWithoutSave(float volume)
	{
		mixer.SetFloat("MusicVolume", volume);
	}

	public void Pause()
	{
		SetSoundVolumeWithoutSave(0);
		SetMusicVolumeWithoutSave(0);
	}
}
