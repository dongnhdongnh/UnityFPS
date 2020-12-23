using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerPrefsOnline
{
	const string SAVEKEY_DataVersion = "SAVEKEY_DataVersion";
	const string SAVEKEY_PlayerPrefsOnline = "SAVEKEY_PlayerPrefsOnline";
	static Dictionary<string, SaveDataObject> dataTemp = new Dictionary<string, SaveDataObject>();
	static List<SaveDataObject> _datas;
	public static List<SaveDataObject> Datas
	{
		get
		{
			if (_datas == null)
				_datas = new List<SaveDataObject>();
			return _datas;
		}
	}

	public static bool HasKey(string key)
	{
		return PlayerPrefs.HasKey(key);
	}

	public static bool GetBool(string key, bool defaultValue)
	{
		int outPut = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0);
		AddKey(key, outPut.ToString(), ValueTypeEnum.INT);
		return outPut == 1;
	}
	public static void SetBool(string key, bool value)
	{
		AddKey(key, value.ToString(), ValueTypeEnum.INT);
		PlayerPrefs.SetInt(key, value ? 1 : 0);
	}


	public static int GetInt(string key, int defaultValue = 0)
	{
		int outPut = PlayerPrefs.GetInt(key, defaultValue);
		AddKey(key, outPut.ToString(), ValueTypeEnum.INT);
		return outPut;
	}
	public static void SetInt(string key, int value)
	{
		AddKey(key, value.ToString(), ValueTypeEnum.INT);
		PlayerPrefs.SetInt(key, value);
	}

	public static float GetFloat(string key, float defaultValue = 0)
	{
		float outPut = PlayerPrefs.GetFloat(key, defaultValue);
		AddKey(key, outPut.ToString(), ValueTypeEnum.FLOAT);
		return outPut;
	}
	public static void SetFloat(string key, float value)
	{
		AddKey(key, value.ToString(), ValueTypeEnum.FLOAT);
		PlayerPrefs.SetFloat(key, value);
	}

	public static string GetString(string key, string defaultValue = "")
	{
		string outPut = PlayerPrefs.GetString(key, defaultValue);
		AddKey(key, outPut, ValueTypeEnum.STRING);
		return outPut;
	}
	public static void SetString(string key, string value)
	{
		AddKey(key, value, ValueTypeEnum.STRING);
		PlayerPrefs.SetString(key, value);
	}

	static void AddKey(string key, string value, ValueTypeEnum valueType)
	{
		SaveDataObject sdo = new SaveDataObject();
		sdo.Key = key;
		sdo.Value = value;
		sdo.ValueType = valueType.ToString();
		if (dataTemp.ContainsKey(key))
			dataTemp[key] = sdo;
		else
			dataTemp.Add(key, sdo);
	}
	public static void Save()
	{
		PlayerPrefs.Save();
	}
	#region DataOnline
	public static void Init()
	{
		if (PlayerPrefs.HasKey(SAVEKEY_PlayerPrefsOnline))
		{
			string _Cache = PlayerPrefs.GetString(SAVEKEY_PlayerPrefsOnline);
			JSONData data = JsonUtility.FromJson<JSONData>(_Cache);
			foreach (SaveDataObject item in data.Datas)
			{
				if (item.Key.Equals(SAVEKEY_PlayerPrefsOnline))
				{
					Debug.LogError("###=>we broke" + item.Value);
					continue;
				}
				if (!dataTemp.ContainsKey(item.Key))
					dataTemp.Add(item.Key, item);
				else
					Debug.LogError(item.Key);
			}
		}
	}
	public static void UploadData()
	{
		JSONData data = new JSONData();
		foreach (string key in dataTemp.Keys)
		{
			//	Debug.LogError(dataTemp[key]);
			if (key.Equals(SAVEKEY_PlayerPrefsOnline)) continue;
			SaveDataObject o = dataTemp[key];
			data.Datas.Add(o);
		}
		string _stringData = JsonUtility.ToJson(data);
		Debug.LogError(data.Datas.Count + "to json :" + JsonUtility.ToJson(data).ToString());
		PlayerPrefs.SetString(SAVEKEY_PlayerPrefsOnline, _stringData);
		PlayerPrefs.Save();
		//GameUtils.FirebaseSetData(_stringData);
	}
	public static void DownloadData()
	{ }
	#endregion
}
[System.Serializable]
public class JSONData
{
	public List<SaveDataObject> Datas = new List<SaveDataObject>();

}
public enum ValueTypeEnum
{
	INT = 0, STRING = 1, FLOAT = 2
}
[System.Serializable]
public class SaveDataObject
{

	public string Key;
	public string Value;
	public string ValueType;
}
