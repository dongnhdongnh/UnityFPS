using UnityEngine;
using System.Collections;
using DG.Tweening;
public class ReplayTween : MonoBehaviour
{
    DOTweenAnimation ani;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnEnable()
    {
        if (ani == null)
            ani = GetComponent<DOTweenAnimation>();
        ani.DORestart();
    }
}
