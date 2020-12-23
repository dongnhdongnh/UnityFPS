using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = (T)FindObjectOfType(typeof(T));

                if (FindObjectsOfType(typeof(T)).Length > 1)
                {
                    return _instance;
                }

                if (_instance == null)
                {
                    GameObject singleton = new GameObject();
                    _instance = singleton.AddComponent<T>();
                    singleton.name = "(singleton) " + typeof(T).ToString();

                    DontDestroyOnLoad(singleton);
                }
            }

            return _instance;
        }
    }

    public virtual void Awake()
    {
        T[] admods = GameObject.FindObjectsOfType<T>();
        if (admods.Length > 1) Destroy(admods[1].gameObject);
    }

}

