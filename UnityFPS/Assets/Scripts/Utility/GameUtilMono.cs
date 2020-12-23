using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUtilMono : Singleton<GameUtilMono>
{
	public void GoogleAdsLoadWithDelay(float delay)
	{
		Debug.LogError("Reward ads reload with delay " + delay);
		StartCoroutine(DoGoogleAdsLoadWithDelay(delay));
	}
	public void GoogleInterLoadWithDelay(float delay)
	{
		Debug.LogError("Reward ads reload with delay " + delay);
		StartCoroutine(DoGoogleAdsLoadWithDelay(delay));
	}
	IEnumerator DoGoogleAdsLoadWithDelay(float delay)
	{
		yield return Yielders.Get(delay);
		//	GameUtils.GoogleAds_LoadRewardedAd();
	}
	IEnumerator DoGoogleInterLoadWithDelay(float delay)
	{
		yield return Yielders.Get(delay);
		//GameUtils.GoogleAds_LoadInterstitialAd();
	}
}
