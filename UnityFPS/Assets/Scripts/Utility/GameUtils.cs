//using UnityEngine;
//using System.Collections;
//using System;
//using GoogleMobileAds.Api;
//using UnityEngine.Purchasing;
//using System.Collections.Generic;
//using Firebase;
//using Firebase.Unity;
//using Firebase.Database;
//using GooglePlayGames.BasicApi;
//using GooglePlayGames;
//using Frictionless;
//using Firebase.Analytics;
//using Facebook.Unity;
//using Firebase.Extensions;
//using System.Threading.Tasks;
//using UnityEngine.Networking;
//using System.Globalization;

//public class GameUtils : SingletonClass<GameUtils>, IStoreListener
//{
//	public static bool isInit = false;
//	public delegate void VoidEvent();
//	public delegate void VoidFloatInputEvent(float input);
//	public static void EventHandlerIni()
//	{

//		ServiceFactory.Instance.Reset();
//		ServiceFactory.Instance.RegisterSingleton<MessageRouter>();
//	}
//	public static void RaiseMessage(object msg)
//	{

//		ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(msg);
//	}

//	public static void AddHandler<T>(Action<T> handler)
//	{

//		ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<T>(handler);
//	}

//	public static void RemoveHandler<T>(Action<T> handler)
//	{
//		ServiceFactory.Instance.Resolve<MessageRouter>().RemoveHandler<T>(handler);
//	}
//	public static void EventHandlerReset()
//	{

//		ServiceFactory.Instance.Reset();
//	}

//	public static void GetInternetTime(MonoBehaviour mono, Action<DateTime> OnSuccessGetTime, Action OnErrorGetTime)
//	{
//		mono.StartCoroutine(DoGetInternetTime(OnSuccessGetTime, OnErrorGetTime));
//	}
//	public static bool GetInternetTimeDone = false;
//	public static bool InternetTimeCheck_DailyGift = false;
//	public static IEnumerator DoGetInternetTime(Action<DateTime> OnSuccessGetTime, Action OnErrorGetTime)
//	{
//		DateTime _preOnlineTime = DateTime.Now;
//		if (GetInternetTimeDone)
//		{
//			DailyGiftController.LastLoginOnline(out _preOnlineTime, _preOnlineTime);
//			if (OnSuccessGetTime != null)
//				OnSuccessGetTime(_preOnlineTime);
//			yield break;
//		}
//		Debug.LogError("Get online time");
//		UnityWebRequest myHttpWebRequest = UnityWebRequest.Get("http://www.google.com");
//		yield return myHttpWebRequest.Send();

//		if (myHttpWebRequest.isNetworkError || myHttpWebRequest.isHttpError || myHttpWebRequest.isNetworkError)
//		{
//			Debug.LogError("Get Time errror");
//			if (OnErrorGetTime != null)
//				OnErrorGetTime();
//			yield break;
//		}
//		string netTime = myHttpWebRequest.GetResponseHeader("date");
//		DateTime timeGet = DateTime.ParseExact(netTime,
//								   "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
//								   CultureInfo.InvariantCulture.DateTimeFormat,
//								   DateTimeStyles.AssumeUniversal).ToLocalTime();
//#if HACK_TIME
//		timeGet = DateTime.Now;
//#endif
//		Debug.LogError(timeGet.ToString() + " was response");
//		if (OnSuccessGetTime != null)
//			OnSuccessGetTime(timeGet);
//		_preOnlineTime = timeGet;
//		if (DailyGiftController.LastLoginOnline(out _preOnlineTime, timeGet))
//		{
//			if (GameUtils.isNewDay(_preOnlineTime, timeGet))
//			{
//				//new day:
//				InternetTime_NewDay(timeGet);
//			}
//			else
//			{
//				//still old day:
//				//set nothing
//			}
//		}
//		else
//		{
//			//have no timeOnline before
//			//new day:
//			InternetTime_NewDay(timeGet);
//		}
//		DailyGiftController.LastLoginOnline_Set(timeGet);
//		//	DailyGiftController.LastLoginOffline_Set(timeGet);
//		GetInternetTimeDone = true;
//	}
//	static void InternetTime_NewDay(DateTime timeGet)
//	{
//		DailyGiftController.LastLoginOnline_Set(timeGet);
//		//	DailyGiftController.LastLoginOffline_Set(timeGet);
//		InternetTimeCheck_DailyGift = true;
//	}
//	#region Time
//	public static bool isNewDay(DateTime oldDay, DateTime newday)
//	{
//		double _dayFromDailyCheck = (double)(newday - oldDay).TotalDays;
//		if (_dayFromDailyCheck > 0) return true;
//		return false;
//	}
//	#endregion

//	#region FB
//	public static void FBInit()
//	{
//		if (FB.IsInitialized)
//		{
//			FB.ActivateApp();
//			//	FBLoadAds();
//		}
//		else
//		{
//			//Handle FB.Init
//			FB.Init(() =>
//			{
//				FB.ActivateApp();
//				//	FBLoadAds();
//			});
//		}
//	}
//	//public static void FBLoadAds()
//	//{
//	//	if (!AudienceNetworkAds.IsInitialized())
//	//		AudienceNetworkAds.Initialize();
//	//	//AdSettings.AddTestDevice("59f798e5-d3dc-41b8-b41c-61182a825317");
//	////	AdSettings.AddTestDevice(AdSettings.d);
//	//	RewardedVideoAdScene.Instance.LoadRewardedVideo();
//	//	InterstitialAdScene.Instance.LoadInterstitial();
//	//}
//	#endregion
//	#region FBAds
//	public const string FBAds_Reward = "1371586266363650_1371586989696911";
//	public const string FBAds_Inters = "1371586266363650_1371587846363492";
//	#endregion
//	#region GoogleAds
//#if UNITY_ANDROID
//	const string appId = "ca-app-pub-3928905068699353~9819730951";
//	const string AdsBannerID = "";
//	const string AdsBannerInters = "ca-app-pub-3928905068699353/8008736086";
//	const string AdsRewards = "ca-app-pub-3928905068699353/3064666639";
//#elif UNITY_IOS
//	const string appId = "ca-app-pub-3928905068699353~7620660521";
//	const string AdsBannerID = "";
//	const string AdsBannerInters = "ca-app-pub-3928905068699353/6307578855";
//	const string AdsRewards = "ca-app-pub-3928905068699353/2368333841"; 
//#else
//    const string appId = "ca-app-pub-3928905068699353~7620660521";
//	const string AdsBannerID = "";
//	const string AdsBannerInters = "ca-app-pub-3928905068699353/6307578855";
//	const string AdsRewards = "ca-app-pub-3928905068699353/2368333841";
//#endif
//	private static RewardedAd rewardedAd;
//	static bool rewardedAdLoading = false;
//	private static GoogleMobileAds.Api.InterstitialAd interstitial;
//	public static void GoogleAdsInit()
//	{
//		//	InterstitialAdScene.Instance.LoadInterstitial();
//		//	RewardedVideoAdScene.Instance.LoadRewardedVideo();
//		//	return;
//		MobileAds.Initialize(appId);
//		GoogleAds_LoadRewardedAd();
//		GoogleAds_LoadInterstitialAd();
//	}
//	public static void GoogleAds_LoadInterstitialAd()
//	{
//		if (interstitial != null) return;
//		interstitial = new GoogleMobileAds.Api.InterstitialAd(AdsBannerInters);
//		AdRequest request = new AdRequest.Builder().Build();
//		interstitial.OnAdLoaded += (s, a) => { Debug.LogError("Inter load done"); };
//		interstitial.OnAdFailedToLoad += (s, a) => { Debug.LogError("Inter fail to load"); interstitial.Destroy(); interstitial = null; GameUtilMono.Instance.GoogleInterLoadWithDelay(3); };
//		interstitial.OnAdClosed += (s, a) => { Debug.LogError("Inter close"); interstitial.Destroy(); interstitial = null; GoogleAds_LoadInterstitialAd(); MusicManager.Instance.ReloadVolume(); };
//		interstitial.LoadAd(request);
//	}

//	public static void GoogleAds_LoadRewardedAd()
//	{
//		if (rewardedAd != null) return;
//		rewardedAd = new RewardedAd(AdsRewards);
//		rewardedAdLoading = true;
//		// Create an empty ad request.
//		AdRequest request = new AdRequest.Builder().Build();
//		// Load the rewarded ad with the request.
//		rewardedAd.LoadAd(request);
//		rewardedAd.OnAdLoaded += (s, a) =>
//		{
//			rewardedAdLoading = false;
//			Debug.Log("rewardedAd.OnAdLoaded Done");
//			//if (OnRewardDone != null)
//			//{
//			//	OnRewardDone();
//			//}
//		};
//		rewardedAd.OnAdFailedToLoad += (s, a) =>
//		{
//			rewardedAdLoading = false;
//			Debug.Log("rewardedAd.OnAdFailedToLoad Failed to load " + a.Message);
//			rewardedAd = null;
//			GameUtilMono.Instance.GoogleAdsLoadWithDelay(3);
//			//GameUtilMono.Instance.StartCoroutine();
//			//	if (OnRewardError != null) OnRewardDone();
//		};
//		rewardedAd.OnAdClosed += (s, a) => { Debug.Log("rewardedAd.OnAdLoaded Close"); rewardedAd = null; GoogleAds_LoadRewardedAd(); };

//	}
//	public static int AdsShowInterTime = 0;
//	public static int AdsShowInterTimeCheckTotal = 0;
//	public static int AdsShowInterTimeTotal = 0;
//	public static void GoogAdsShowInters()
//	{
//		if (GameSave.RemoveAds) return;
//		AdsShowInterTime++;
//		AdsShowInterTimeCheckTotal++;

//		if (AdsShowInterTimeCheckTotal <= 2)
//		{
//			if (AdsShowInterTimeTotal == 0)
//			{
//				//1,2
//				if (AdsShowInterTimeCheckTotal == 1)
//				{
//					if (UnityEngine.Random.Range(0, 2) == 0)
//						DoGoogAdsShowInters();
//				}
//				else
//					DoGoogAdsShowInters();
//			}
//		}
//		else
//		if (AdsShowInterTime >= 3)
//		{
//			DoGoogAdsShowInters();
//		}

//	}
//	static void DoGoogAdsShowInters()
//	{
//		if (interstitial.IsLoaded())
//		{
//			Debug.LogError("Show inter Ads");
//			interstitial.Show();
//			MusicManager.Instance.Pause();
//			FirebaseLog_CustomEvent("GoogAdsShowInters");
//			//InterstitialAdScene.Instance.LoadInterstitial();
//			AdsShowInterTime = 0;
//			AdsShowInterTimeTotal++;
//		}
//	}

//	public void DOEndOfFrameEvent(MonoBehaviour mono, VoidEvent voidEvent)
//	{
//		mono.StartCoroutine(IEEndOfFrameEvent(voidEvent));
//	}
//	IEnumerator IEEndOfFrameEvent(VoidEvent voidEvent)
//	{
//		yield return new WaitForEndOfFrame();
//		if (voidEvent != null)
//			voidEvent();
//	}
//	public static void GoogleAdsRewardAds(VoidEvent OnRewardDone, VoidEvent OnRewardError, MonoBehaviour mono = null)
//	{

//#if UNITY_EDITOR
//		OnRewardDone();
//		return;
//#endif
//		//RewardedVideoAdScene.Instance.ShowRewardedVideo(OnRewardDone, OnRewardError);
//		//return;

//		if (rewardedAd.IsLoaded())
//		{
//			rewardedAd.OnUserEarnedReward += (sender, reward) =>
//			{
//				string type = reward.Type;
//				double amount = reward.Amount;
//				FirebaseLog_CustomEvent("GoogleAdsRewardAds");
//				if (OnRewardDone != null)
//				{
//					if (mono != null)
//						GameUtils.Instance.DOEndOfFrameEvent(mono, OnRewardDone);
//					else
//						OnRewardDone();
//				}
//				MusicManager.Instance.ReloadVolume();
//			};
//			rewardedAd.OnAdFailedToLoad += (s, a) =>
//			{
//				Debug.Log("rewardedAd.OnAdFailedToLoad Failed to load " + a.Message);
//				if (OnRewardError != null)
//					if (mono != null)
//						GameUtils.Instance.DOEndOfFrameEvent(mono, OnRewardError);
//					else
//						OnRewardError();
//				MusicManager.Instance.ReloadVolume();

//			};
//			rewardedAd.Show();
//			MusicManager.Instance.Pause();
//		}
//		else
//		{
//			Debug.LogError("rewardedAd not IsLoaded");
//			if (OnRewardError != null) OnRewardError();

//		}
//	}

//	#endregion
//	#region GooglePLay
//	public static string UserID = "";
//	public static void GooglePlayInit()
//	{
//#if UNITY_ANDROID
//		PlayGamesClientConfiguration config = new
//		//	  PlayGamesClientConfiguration.Builder().RequestServerAuthCode(false).Build();
//		PlayGamesClientConfiguration.Builder().RequestEmail().Build();
//		PlayGamesPlatform.InitializeInstance(config);
//		PlayGamesPlatform.DebugLogEnabled = true;
//		PlayGamesPlatform.Activate();
//		Debug.LogError("ggplay init");
//#endif
//	}
//	public static void GooglePlayLogin()
//	{
//#if UNITY_ANDROID
//		if (Social.localUser.authenticated)
//		{
//			GooglePlayGames.OurUtils.PlayGamesHelperObject.RunOnGameThread(
//   () =>
//   {
//	   Debug.Log("Not need Login,Local user's email is " +
//			((PlayGamesLocalUser)Social.localUser).Email);
//	   UserID = ((PlayGamesLocalUser)Social.localUser).Email;
//	   PlayerPrefsOnline.UploadData();
//	   // use the email as needed
//   });
//			return;
//		}
//		Debug.LogError("not login,google play have to login");
//		Social.localUser.Authenticate((bool success, string error) =>
//	{
//		if (success)
//		{

//			GooglePlayGames.OurUtils.PlayGamesHelperObject.RunOnGameThread(
//   () =>
//   {
//	   Debug.Log("Local user's email is " +
//			((PlayGamesLocalUser)Social.localUser).Email);
//	   UserID = ((PlayGamesLocalUser)Social.localUser).Email;
//	   PlayerPrefsOnline.UploadData();
//	   // use the email as needed
//   });
//			Debug.Log("Login Sucess:" + UserID);

//		}
//		else
//		{
//			Debug.Log("Login failed:" + error);
//		}
//	});
//#endif
//	}
//	public static void GooglePlayOnShowLeaderBoard(string boardName)
//	{
//#if UNITY_ANDROID
//		//        Social.ShowLeaderboardUI (); // Show all leaderboard
//		((PlayGamesPlatform)Social.Active).ShowLeaderboardUI(boardName);
//		// Show current (Active) leaderboard
//#endif
//	}
//	public static void GooglePlayAddScoreToLeaderBorad(int score, string boardName)
//	{
//		if (Social.localUser.authenticated)
//		{
//			Social.ReportScore(score, boardName, (bool success) =>
//			{
//				if (success)
//				{
//					Debug.Log("Update Score Success");

//				}
//				else
//				{
//					Debug.Log("Update Score Fail");
//				}
//			});
//		}
//	}
//	/// <summary>
//	/// On Logout of your Google+ Account
//	/// </summary>
//	public static void GooglePlayLogOut()
//	{
//#if UNITY_ANDROID
//		((PlayGamesPlatform)Social.Active).SignOut();
//#endif
//	}
//	#endregion
//	#region Firebase&GGPlay
//	//public static Firebase.Auth.FirebaseUser CurrentFirebaseUser;
//	public static void FirebaseRemoteConfigInit()
//	{
//		//Set default value:
//		System.Collections.Generic.Dictionary<string, object> defaults =
//		  new System.Collections.Generic.Dictionary<string, object>();

//		// These are the values that are used if we haven't fetched data from the
//		// server
//		// yet, or if we ask for values that the server doesn't have:
//		defaults.Add(FirebaseConfigKey.PercentHPWhenContinue, 50);
//		defaults.Add(FirebaseConfigKey.DiamondWhenContinue, 100);
//		defaults.Add(FirebaseConfigKey.UndeadTimeWhenContinue, 10);
//		defaults.Add(FirebaseConfigKey.SaleData, "");
//		Firebase.RemoteConfig.FirebaseRemoteConfig.SetDefaults(defaults);
//		//GameSave.SaleData = GameData.SaleDataFromFireBase;
//		//GameData.Instance.InitSaleIAP();
//		//get OnlineData:
//		Debug.Log("Firebase Fetching data...");
//		System.Threading.Tasks.Task fetchTask = Firebase.RemoteConfig.FirebaseRemoteConfig.FetchAsync(
//			TimeSpan.Zero);
//		fetchTask.ContinueWithOnMainThread(FetchComplete);
//	}

//	private static void FetchComplete(Task fetchTask)
//	{
//		if (fetchTask.IsCanceled)
//		{
//			Debug.Log("Fetch canceled.");
//		}
//		else if (fetchTask.IsFaulted)
//		{
//			Debug.Log("Fetch encountered an error.");
//		}
//		else if (fetchTask.IsCompleted)
//		{
//			Debug.Log("Fetch completed successfully!");
//		}

//		var info = Firebase.RemoteConfig.FirebaseRemoteConfig.Info;
//		switch (info.LastFetchStatus)
//		{
//			case Firebase.RemoteConfig.LastFetchStatus.Success:
//				Firebase.RemoteConfig.FirebaseRemoteConfig.ActivateFetched();
//				GameSave.SaleData = GameData.SaleDataFromFireBase;
//				Debug.LogError("Sale Data: " + GameSave.SaleData);
//				//GameData.Instance.InitSaleIAP();
//				Debug.Log(String.Format("Remote data loaded and ready (last fetch time {0}).",
//									   info.FetchTime));
//				break;
//			case Firebase.RemoteConfig.LastFetchStatus.Failure:
//				switch (info.LastFetchFailureReason)
//				{
//					case Firebase.RemoteConfig.FetchFailureReason.Error:
//						Debug.Log("Fetch failed for unknown reason");
//						break;
//					case Firebase.RemoteConfig.FetchFailureReason.Throttled:
//						Debug.Log("Fetch throttled until " + info.ThrottledEndTime);
//						break;
//				}
//				break;
//			case Firebase.RemoteConfig.LastFetchStatus.Pending:
//				Debug.Log("Latest Fetch call still pending.");
//				break;
//		}

//		Debug.LogError("Firebase fetch Data done,undead time = " + Firebase.RemoteConfig.FirebaseRemoteConfig.GetValue(FirebaseConfigKey.UndeadTimeWhenContinue).StringValue);
//	}

//	public static long FirebaseRemoteConfigGetLongValue(string valueKey)
//	{
//		return Firebase.RemoteConfig.FirebaseRemoteConfig.GetValue(valueKey).LongValue;
//	}

//	public static string FirebaseRemoteConfigGetStringValue(string valueKey)
//	{
//		return Firebase.RemoteConfig.FirebaseRemoteConfig.GetValue(valueKey).StringValue;
//	}

//	public static bool FirebaseInitDone = false;
//	public static void FirebaseInit(VoidEvent OnGetUserDoneEvent = null)
//	{
//		//PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
//		//	.RequestServerAuthCode(false /* Don't force refresh */)
//		//	.Build();

//		//PlayGamesPlatform.InitializeInstance(config);
//		//PlayGamesPlatform.Activate();

//		//	firebaseAnalytics = FirebaseAnalytics.get
//		Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
//		{
//			var dependencyStatus = task.Result;
//			if (dependencyStatus == Firebase.DependencyStatus.Available)
//			{
//				// Create and hold a reference to your FirebaseApp,
//				// where app is a Firebase.FirebaseApp property of your application class.
//				//   app = Firebase.FirebaseApp.DefaultInstance;

//				// Set a flag here to indicate whether Firebase is ready to use by your app.
//				FirebaseInitDone = true;
//				Debug.LogError("Firebase Init Done");
//				//	FirebaseLoginAndGetCurrentUser(OnGetUserDoneEvent);
//			}
//			else
//			{
//				UnityEngine.Debug.LogError(System.String.Format(
//				  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
//				// Firebase Unity SDK is not safe to use here.

//			}
//			GameUtils.FirebaseRemoteConfigInit();
//		});
//	}
//	// public static void FirebaseLoginAndGetCurrentUser(VoidEvent OnGetUserDoneEvent = null)
//	// {
//	// 	Debug.LogError("Firebase FirebaseLoginAndGetCurrentUser");
//	// 	Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
//	// 	Firebase.Auth.FirebaseUser user = auth.CurrentUser;
//	// 	if (user != null)
//	// 	{
//	// 		Debug.LogError("user take " + user.UserId);
//	// 		//	CurrentFirebaseUser = user;
//	// 		if (OnGetUserDoneEvent != null) OnGetUserDoneEvent();
//	// 	}
//	// 	else
//	// 	{
//	// 		Debug.LogError("Have no User,Sicoal Au");
//	// 		Social.localUser.Authenticate((bool success, string error) =>
//	// 		{
//	// 			Debug.LogError("Social Authen " + success + "error " + error);
//	// 			if (success)
//	// 			{
//	// 				// handle success or failure
//	// 				string authCode = PlayGamesPlatform.Instance.GetServerAuthCode();
//	// 				Debug.LogError("Auth code " + authCode);
//	// 				//Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
//	// 				Firebase.Auth.Credential credential =
//	// 					Firebase.Auth.PlayGamesAuthProvider.GetCredential(authCode);
//	// 				auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
//	// 				{
//	// 					if (task.IsCanceled)
//	// 					{
//	// 						Debug.LogError("SignInWithCredentialAsync was canceled.");
//	// 						return;
//	// 					}
//	// 					if (task.IsFaulted)
//	// 					{
//	// 						Debug.LogError("SignInWithCredentialAsync encountered an error: " + task.Exception);
//	// 						return;
//	// 					}

//	// 					Firebase.Auth.FirebaseUser newUser = task.Result;
//	// 					//	CurrentFirebaseUser = newUser;
//	// 					if (OnGetUserDoneEvent != null) OnGetUserDoneEvent();
//	// 					Debug.LogError("===> user Siged in " + newUser.UserId);
//	// 					Debug.LogFormat("===> User signed in successfully: {0} ({1})",
//	// 						newUser.DisplayName, newUser.UserId);
//	// 				});
//	// 			}
//	// 			else
//	// 			{
//	// 				Debug.LogError("Authenticate Error");
//	// 			}

//	// 		});
//	// 	}
//	// }
//	public static void FirebaseLog_LevelStoryEnd(int chapterName, int levelName, bool isWin, int star, bool fistWin)
//	{
//		try
//		{
//			Debug.LogError("<color=green>Firebase Log event</color>:LevelStoryEnd:" + chapterName + "-" + levelName + "_is Win " + isWin + "_star " + star);
//			string LevelStringName = chapterName + "_" + levelName;
//			Firebase.Analytics.Parameter[] LevelEndParameters = {
//  new Firebase.Analytics.Parameter(Firebase.Analytics.FirebaseAnalytics.ParameterLevel, LevelStringName),
//  new Firebase.Analytics.Parameter("isWin", isWin?1:0),
//  new Firebase.Analytics.Parameter("star", star)};
//			Firebase.Analytics.FirebaseAnalytics.LogEvent(
//			  Firebase.Analytics.FirebaseAnalytics.EventLevelEnd,
//			  LevelEndParameters);
//#if UNITY_EDITOR
//			Debug.LogError("Log =>" + "STORY_" + LevelStringName + "_Win_" + isWin + "_Star_" + star);
//#endif
//			Firebase.Analytics.FirebaseAnalytics.LogEvent(
//				  "STORY_" + LevelStringName + "_Win_" + isWin + "_Star_" + star);
//			Firebase.Analytics.FirebaseAnalytics.LogEvent("STORY_" + LevelStringName + "_Win_" + isWin);
//			if (fistWin)
//			{
//				Firebase.Analytics.FirebaseAnalytics.LogEvent("STORY_FirstTime_" + LevelStringName + "_Win_" + isWin);
//#if UNITY_EDITOR
//				Debug.LogError("STORY_FirstTime_" + LevelStringName + "_Win_" + isWin);
//#endif
//			}
//			if (FB.IsInitialized)
//				FB.LogAppEvent("STORY_" + LevelStringName + "_Win_" + isWin);
//		}
//		catch (Exception ex)
//		{

//			Debug.LogError("catch " + ex.ToString());
//		}


//	}
//	public static void FirebaseLog_LevelSurvivalEnd(int lastRound)
//	{
//#if UNITY_EDITOR
//		return;
//#endif
//		Firebase.Analytics.Parameter[] LevelEndParameters = {
//  new Firebase.Analytics.Parameter(Firebase.Analytics.FirebaseAnalytics.ParameterLevel, GameSave.Endless_LastRound),
// // new Firebase.Analytics.Parameter("isWin", isWin?1:0),
////  new Firebase.Analytics.Parameter("star", star)
//};
//		Firebase.Analytics.FirebaseAnalytics.LogEvent(
//		"Survival_End",
//		  LevelEndParameters);
//		Firebase.Analytics.FirebaseAnalytics.LogEvent(
//				  "Survival_End_Round_" + lastRound);
//		FB.LogAppEvent("Survival_End_Round_" + lastRound);
//	}
//	public static void FirebaseLog_CustomEvent(string customLog)
//	{
//#if UNITY_EDITOR
//		Debug.LogError("<color=green>Firebase_CustomLog:</color>" + customLog);
//		return;
//#endif
//		Firebase.Analytics.FirebaseAnalytics.LogEvent(customLog);
//	}

//	public static void FirebaseLog_CustomEvent_BuySuccess(string data)
//	{
//		GameUtils.FirebaseLog_CustomEvent("BuySuccess_" + makeStringCool(data));
//	}
//	public static string makeStringCool(string input)
//	{
//		input = input.Trim();
//		input = input.Replace(' ', '_');
//		return input;
//	}
//	public static void FirebaseSetData(string data)
//	{
//		if (!FirebaseInitDone)
//		{
//			Debug.LogError("Firebase not init");
//			FirebaseInit(() => { DoFireBaseSetData(data); });
//			return;
//		}
//		//if (CurrentFirebaseUser == null)
//		//{
//		//	Debug.LogError("FireBase user null");
//		//	FirebaseLoginAndGetCurrentUser(() => { DoFireBaseSetData(data); });
//		//	return;
//		//}
//		//if (FirebaseInitDone)
//		//{
//		DoFireBaseSetData(data);
//		//}
//		//else FirebaseInit();
//	}
//	static void DoFireBaseSetData(string data)
//	{
//		if (!FirebaseInitDone)
//		{
//			Debug.LogError("Firebase not init");
//			return;
//		}
//		//if (CurrentFirebaseUser == null)
//		//{
//		//	Debug.LogError("FireBase user null");
//		//	//	FirebaseLoginAndGetCurrentUser();
//		//}
//		if (!(UserID.Trim().Length > 0))
//		{
//			Debug.LogError("UserID not get,not up data to firebase");
//			return;
//		}
//		if (FirebaseInitDone)
//		{
//			FirebaseApp.DefaultInstance.Options.DatabaseUrl = new Uri("https://summon-heroes-new-era-5039699.firebaseio.com/");
//			//FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://summon-heroes-new-era-5039699.firebaseio.com/");
//			DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
//			reference.Child("users").Child(UserID).SetRawJsonValueAsync(data);
//			//Dialog.Setup().ShowConfirm("", "Upload Data Done", () =>
//			//{
//			//	Dialog.Setup().Hide();
//			//});
//			Debug.LogError("Firebase Update data done");
//		}
//	}
//	public static void FirebaseGetData()
//	{
//		if (!(UserID.Trim().Length > 0))
//		{
//			Debug.LogError("UserID not get");
//			return;
//		}
//		FirebaseApp.DefaultInstance.Options.DatabaseUrl = new Uri("https://summon-heroes-new-era-5039699.firebaseio.com/");
//		//	FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://summon-heroes-new-era-5039699.firebaseio.com/");
//		DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;

//		reference.Child("users").Child(UserID).GetValueAsync().ContinueWith(task =>
//			 {
//				 if (task.IsFaulted)
//				 {
//					 // Handle the error...
//				 }
//				 else if (task.IsCompleted)
//				 {
//					 DataSnapshot snapshot = task.Result;
//					 // Do something with snapshot...
//					 Debug.LogError(snapshot.Value);
//				 }
//			 });
//	}
//	#endregion
//	#region UnityVideoReward
//	//	private RewardBasedVideoAd rewardBasedVideo;
//	//	VoidEvent OnAdsDone;
//	//	public void VideoReward_Init()
//	//	{
//	//		return;
//	//		this.rewardBasedVideo = RewardBasedVideoAd.Instance;
//	//		this.rewardBasedVideo.OnAdRewarded += HandleRewardBasedVideoRewarded;
//	//	}

//	//	private void HandleRewardBasedVideoRewarded(object sender, Reward e)
//	//	{
//	//		if (this.OnAdsDone != null) OnAdsDone();
//	//	}

//	//	public void VideoReward_Request(VoidEvent OnAdsDone)
//	//	{
//	//		this.OnAdsDone = OnAdsDone;
//	//#if UNITY_EDITOR
//	//		this.OnAdsDone();
//	//#else
//	//#if UNITY_ANDROID
//	//		string adUnitId = "ca-app-pub-3940256099942544/5224354917";
//	//#elif UNITY_IPHONE
//	//            string adUnitId = "ca-app-pub-3940256099942544/1712485313";
//	//#else
//	//            string adUnitId = "unexpected_platform";
//	//#endif
//	//		// Create an empty ad request.
//	//		AdRequest request = new AdRequest.Builder().Build();
//	//		// Load the rewarded video ad with the request.
//	//		this.rewardBasedVideo.LoadAd(request, adUnitId);
//	//#endif
//	//	}
//	#endregion
//	#region IAPP
//	private IStoreController m_Controller;
//	private IExtensionProvider extensions;
//	private IAppleExtensions m_AppleExtensions;
//	public static List<SubscriptionInfo> SubscriptionInfos;
//	private bool m_PurchaseInProgress;
//	public static bool IAPinitializationComplete = false;

//	#region Subcription
//	private bool checkIfProductIsAvailableForSubscriptionManager(string receipt)
//	{
//		var receipt_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(receipt);
//		if (!receipt_wrapper.ContainsKey("Store") || !receipt_wrapper.ContainsKey("Payload"))
//		{
//			Debug.Log("The product receipt does not contain enough information");
//			return false;
//		}
//		var store = (string)receipt_wrapper["Store"];
//		var payload = (string)receipt_wrapper["Payload"];

//		if (payload != null)
//		{
//			switch (store)
//			{
//				case GooglePlay.Name:
//					{
//						var payload_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(payload);
//						if (!payload_wrapper.ContainsKey("json"))
//						{
//							Debug.Log("The product receipt does not contain enough information, the 'json' field is missing");
//							return false;
//						}
//						var original_json_payload_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode((string)payload_wrapper["json"]);
//						if (original_json_payload_wrapper == null || !original_json_payload_wrapper.ContainsKey("developerPayload"))
//						{
//							Debug.Log("The product receipt does not contain enough information, the 'developerPayload' field is missing");
//							return false;
//						}
//						var developerPayloadJSON = (string)original_json_payload_wrapper["developerPayload"];
//						var developerPayload_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(developerPayloadJSON);
//						if (developerPayload_wrapper == null || !developerPayload_wrapper.ContainsKey("is_free_trial") || !developerPayload_wrapper.ContainsKey("has_introductory_price_trial"))
//						{
//							Debug.Log("The product receipt does not contain enough information, the product is not purchased using 1.19 or later");
//							return false;
//						}
//						return true;
//					}
//				case AppleAppStore.Name:
//				case AmazonApps.Name:
//				case MacAppStore.Name:
//					{
//						return true;
//					}
//				default:
//					{
//						return false;
//					}
//			}
//		}
//		return false;
//	}
//	#endregion

//	public void InAppInit()
//	{
//		Debug.LogError("Inapp Init");
//		var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
//		List<IAPObject> iapObjects = GameData.Instance.InitIAPIDs();
//		//builder.AddProduct("xyz", ProductType.Consumable, new IDs
//		//{
//		//	{"100_gold_coins_google", GooglePlay.Name},
//		//	{"100_gold_coins_mac", MacAppStore.Name}
//		//});
//		foreach (IAPObject item in iapObjects)
//		{
//			builder.AddProduct(item.IAPID, item.IAPproductType);
//		}
//		UnityPurchasing.Initialize(this, builder);
//	}
//	public void IntializeNewProduct(string[] productIds)
//	{
//		HashSet<ProductDefinition> itemsHashSet = new HashSet<ProductDefinition>();
//		foreach (string productId in productIds)
//		{
//			if (GetIAPProduct(productId) != null)
//			{
//				//already have this product,return
//				return;
//			}
//			itemsHashSet.Add(new ProductDefinition(productId, ProductType.NonConsumable));
//		}
//		if (itemsHashSet.Count > 0)
//			m_Controller.FetchAdditionalProducts(itemsHashSet, OnProductsFetched, OnInitializeFailed);
//	}

//	private void OnProductsFetched()
//	{
//		Debug.Log("OnProductsFetched");
//		GameUtils.RaiseMessage(GameEvent.OnIAPInitialized.Instance);

//	}


//	public void OnInitializeFailed(InitializationFailureReason error)
//	{
//		//Dialog.Setup().ShowConfirm(GameData.Instance.LocalizationText("InitializationFailureReason"), error.ToString(), () =>
//		//{
//		//	Dialog.Setup().Hide();
//		//});
//		//throw new NotImplementedException();
//	}
//	List<IIAPButton> IAPButtons = new List<IIAPButton>();
//	public void AddIAPButton(IIAPButton button)
//	{
//		if (!IAPButtons.Contains(button))
//			IAPButtons.Add(button);
//	}
//	public void RemoveIAPButton(IIAPButton button)
//	{
//		IAPButtons.Remove(button);
//	}
//	public Product GetIAPProduct(string productID)
//	{
//		if (m_Controller != null && m_Controller.products != null && !string.IsNullOrEmpty(productID))
//		{
//			return m_Controller.products.WithID(productID);
//		}
//		Debug.LogError("CodelessIAPStoreListener attempted to get unknown product " + productID);
//		return null;
//	}
//	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
//	{
//		PurchaseProcessingResult result;
//		Debug.Log("Purchase OK: " + e.purchasedProduct.definition.id);
//		Debug.Log("Receipt: " + e.purchasedProduct.receipt);
//		m_PurchaseInProgress = false;
//		GameSave.IAPTime++;
//		if (!GameSave.IAPToday)
//		{
//			GameSave.IAPTimeDay++;
//			GameSave.IAPToday = true;
//		}
//		foreach (IIAPButton button in IAPButtons)
//		{
//			if (button.MyIAPObject.IAPID == e.purchasedProduct.definition.id)
//			{
//				result = button.ProcessPurchase(e);

//			}
//		}
//		GameEvent.OnIAPProcessPurchase.Instance.product = e.purchasedProduct;
//		GameUtils.RaiseMessage(GameEvent.OnIAPProcessPurchase.Instance);
//		string userID = "unknow";
//		if (GameUtils.UserID != null)
//			userID = GameUtils.UserID;
//		if (userID.Trim().Length <= 0) userID = "unknow";
//		userID = userID.Replace('@', 'a');
//		FirebaseLog_CustomEvent("IAP_ProcessPurchase_" + e.purchasedProduct.definition.id + "_user_" + userID);

//		return PurchaseProcessingResult.Complete;
//	}

//	public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
//	{
//		Debug.Log("Purchase failed: " + product.definition.id);
//		m_PurchaseInProgress = false;
//		//Dialog.Setup().ShowConfirm(GameData.Instance.LocalizationText(""), GameData.Instance.LocalizationText("Product purchase failed"), () =>
//		//{
//		//	Dialog.Setup().Hide();
//		//});
//		foreach (IIAPButton button in IAPButtons)
//		{
//			if (button.MyIAPObject.IAPID == product.definition.id)
//			{
//				button.OnPurchaseFailed(product, reason);
//				//resultProcessed = true;
//			}
//		}
//	}

//	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
//	{
//		Debug.LogError("===================>IAP OnInitiablzed");
//		this.m_Controller = controller;
//		m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();
//		this.extensions = extensions;
//		IAPinitializationComplete = true;

//		SubscriptionInfos = new List<SubscriptionInfo>();
//		Dictionary<string, string> introductory_info_dict = m_AppleExtensions.GetIntroductoryPriceDictionary();
//		foreach (var item in this.m_Controller.products.all)
//		{
//			if (item.availableToPurchase)
//			{
//				if (item.receipt != null && item.definition.type == ProductType.Subscription)
//				{
//					IAPObject _currentIAPObject = null;
//					foreach (IAPObject iapObject in GameData.Instance.InitIAPIDs())
//					{
//						if (iapObject.IAPID.Equals(item.definition.id))
//						{
//							_currentIAPObject = iapObject; break;
//						}
//					}
//					if (_currentIAPObject == null)
//					{
//						Debug.LogError("Not found IAP ID on gameData " + item.definition.id);
//						continue;
//					}
//					if (checkIfProductIsAvailableForSubscriptionManager(item.receipt))
//					{
//						string intro_json = (introductory_info_dict == null || !introductory_info_dict.ContainsKey(item.definition.storeSpecificId)) ? null : introductory_info_dict[item.definition.storeSpecificId];
//						SubscriptionManager p = new SubscriptionManager(item, intro_json);
//						SubscriptionInfo info = p.getSubscriptionInfo();
//						SubscriptionInfos.Add(info);
//						Debug.Log("product id is: " + info.getProductId());
//						Debug.Log("purchase date is: " + info.getPurchaseDate());
//						Debug.Log("subscription next billing date is: " + info.getExpireDate());
//						Debug.Log("is subscribed? " + info.isSubscribed().ToString());
//						Debug.Log("is expired? " + info.isExpired().ToString());
//					}
//					else
//					{
//						Debug.Log("This product is not available for SubscriptionManager class, only products that are purchase by 1.19+ SDK can use this class.");
//					}
//				}
//			}
//		}

//		GameUtils.RaiseMessage(GameEvent.OnIAPInitialized.Instance);
//	}

//	public void PurchaseButtonClick(string productID)
//	{
//		if (m_PurchaseInProgress == true)
//		{
//			Debug.Log("Please wait, purchase in progress");
//			return;
//		}

//		if (m_Controller == null)
//		{
//			Debug.LogError("Purchasing is not initialized");
//			Dialog.Setup().ShowConfirm(GameData.Instance.LocalizationText("Error"), GameData.Instance.LocalizationText("Something went wrong.Please check your connection and try again later"), () =>
//			{
//				Dialog.Setup().Hide();
//			});
//			//	this.OnPurchaseFailed();
//			foreach (var button in IAPButtons)
//			{
//				if (button.MyIAPObject.IAPID == productID)
//				{
//					button.OnPurchaseFailed(null, UnityEngine.Purchasing.PurchaseFailureReason.PurchasingUnavailable);
//				}
//			}
//			return;
//		}

//		if (m_Controller.products.WithID(productID) == null)
//		{
//			Debug.LogError("No product has id " + productID);
//			Dialog.Setup().ShowConfirm(GameData.Instance.LocalizationText("Error"), "No product has id " + productID, () =>
//			{
//				Dialog.Setup().Hide();
//			});
//			foreach (var button in IAPButtons)
//			{
//				if (button.MyIAPObject.IAPID == productID)
//				{
//					button.OnPurchaseFailed(null, UnityEngine.Purchasing.PurchaseFailureReason.ProductUnavailable);
//				}
//			}
//			return;
//		}

//		// Don't need to draw our UI whilst a purchase is in progress.
//		// This is not a requirement for IAP Applications but makes the demo
//		// scene tidier whilst the fake purchase dialog is showing.
//		m_PurchaseInProgress = true;

//		//Sample code how to add accountId in developerPayload to pass it to getBuyIntentExtraParams
//		//Dictionary<string, string> payload_dictionary = new Dictionary<string, string>();
//		//payload_dictionary["accountId"] = "Faked account id";
//		//payload_dictionary["developerPayload"] = "Faked developer payload";
//		//m_Controller.InitiatePurchase(m_Controller.products.WithID(productID), MiniJson.JsonEncode(payload_dictionary));
//		m_Controller.InitiatePurchase(m_Controller.products.WithID(productID));

//	}
//	public static void MakeSalePack()
//	{
//		ShopPackOneData _presaleItem = null;
//		try
//		{
//			_presaleItem = JsonUtility.FromJson<ShopPackOneData>(GameSave.SaleDataFake);
//		}
//		catch (Exception ex)
//		{

//			_presaleItem = null;
//		}
//		if (_presaleItem != null)
//		{
//			try
//			{
//				DateTime _timePreSale = DateTime.Parse(_presaleItem.time_sale);
//				if (DateTime.Now.Date == _timePreSale.Date)
//				{
//					Debug.LogError("Already Sale: not create " + _presaleItem.time_sale + "pack type:" + _presaleItem.packType + "_id:" + _presaleItem.ID);
//					return;
//				}
//			}
//			catch (Exception ex)
//			{

//				//throw;
//			}

//		}

//		//Create New Shop Pack:

//		List<ShopPackOneData> _shopPacks = new List<ShopPackOneData>();
//		foreach (ShopPackOneData item in GameData.Instance.ShopData_Pack.datas)
//		{
//			if (!GameSave.Pack_isBuy_Get(PackType.OneTimePack, item.ID))
//			{
//				item.packType = PackType.OneTimePack;
//				_shopPacks.Add(item);
//			}
//		}
//		foreach (ShopPackOneData item in GameData.Instance.ShopData_Pack_week.datas)
//		{
//			if (!GameSave.Pack_isBuy_Get(PackType.WeeklyPack, item.ID))
//			{
//				item.packType = PackType.WeeklyPack;
//				_shopPacks.Add(item);
//			}
//		}
//		foreach (ShopPackOneData item in GameData.Instance.ShopData_Pack_month.datas)
//		{
//			if (!GameSave.Pack_isBuy_Get(PackType.MonthlyPack, item.ID))
//			{
//				item.packType = PackType.MonthlyPack;
//				_shopPacks.Add(item);
//			}
//		}
//		if (_shopPacks.Count > 0)
//		{
//			ShopPackOneData _saleItem = null;

//			//Case have sale on firebase:
//			ShopSaleInfor _saleFronFB = GameData.Instance.SaleDataFromFB;
//			if (_saleFronFB != null && _saleFronFB.datas != null && _saleFronFB.datas.Length > 0)
//			{
//				ShopSaleInforData _curretSale = _saleFronFB.datas[0];
//				Debug.LogError("_From firebase,wwe sale " + _curretSale.IAPID + "/" + _curretSale.packID);
//				foreach (ShopPackOneData _shopPack in _shopPacks)
//				{
//					if (_shopPack.IAPObject.IAPID.Equals(_curretSale.IAPID))
//					{
//						_saleItem = _shopPack;
//						break;
//					}
//				}
//			}
//			if (_saleItem == null)
//			{
//				//Case have no sale on FireBase:
//				int _randomPackID = UnityEngine.Random.Range(0, _shopPacks.Count);
//				_saleItem = _shopPacks[_randomPackID];
//			}
//			DateTime _toDayEndSale = DateTime.Now;
//			_saleItem.time_sale = _toDayEndSale.ToString();
//			Debug.LogError("make saleItem " + JsonUtility.ToJson(_saleItem));
//			GameSave.SaleDataFake = JsonUtility.ToJson(_saleItem);
//		}

//	}

//	public static void MakeSaleHeroPack()
//	{
//		ShopPackOneData _presaleItem = null;
//		try
//		{
//			_presaleItem = JsonUtility.FromJson<ShopPackOneData>(GameSave.SaleDataFakeHero);
//		}
//		catch (Exception ex)
//		{

//			_presaleItem = null;
//		}
//		if (_presaleItem != null)
//		{
//			try
//			{
//				DateTime _timePreSale = DateTime.Parse(_presaleItem.time_sale);
//				if (DateTime.Now.Date == _timePreSale.Date)
//				{
//					Debug.LogError("Already Hero Sale: not create " + _presaleItem.time_sale + " hero id:" + _presaleItem.ID);
//					return;
//				}
//			}
//			catch (Exception ex)
//			{

//				//throw;
//			}

//		}


//		List<HeroData> heros = GameData.Instance.HeroDatas;
//		HeroData heroToSale = null;
//		foreach (HeroData hero in heros)
//		{
//			if (!hero.isUnlocked)
//			{
//				heroToSale = hero; break;
//			}
//		}
//		if (heroToSale == null) return;
//		ShopPackOneData _herosaleData = new ShopPackOneData();
//		_herosaleData.ID = heroToSale.ID;
//		DateTime _toDayEndSale = DateTime.Now;
//		_herosaleData.time_sale = _toDayEndSale.ToString();
//		GameSave.SaleDataFakeHero = JsonUtility.ToJson(_herosaleData);
//	}

//	public void GetSubscription(ItemData itemData)
//	{
//		SubscriptionInfo subItemNow = null;
//		foreach (SubscriptionInfo subItem in SubscriptionInfos)
//		{
//			if (itemData.IAPObject.IAPID.Equals(subItem.getProductId()))
//			{
//				subItemNow = subItem;
//				break;
//			}
//		}
//		if (subItemNow == null)
//		{ Debug.LogError("NOT GET " + itemData.IAPObject.IAPID); }
//		else
//		{
//			int _dateGet = GameSave.IAPSubDay_LastGet_Get(itemData.IAPObject.IAPID);
//			int _dayToEnd = subItemNow.getRemainingTime().Days;
//			if (_dateGet > _dayToEnd)
//			{
//				// can get
//				GetSubcriptionItem(itemData);
//			}
//			GameSave.IAPSubDay_LastGet_Set(itemData.IAPObject.IAPID, _dayToEnd);
//		}
//	}
//	public void GetSubcriptionItem(ItemData itemData)
//	{
//		if (itemData.itemInforDays == null || itemData.itemInforDays.Length == 0)
//		{
//			Dialog.Setup().ShowConfirm("", "Nothing to get", () =>
//			{
//				Dialog.Setup().Hide();
//			});
//			return;
//		}
//		string output = "";
//		for (int i = 0; i < itemData.itemInforDays.Length; i++)
//		{
//			ItemData _itemData = itemData.itemInforDays[i];
//			int amountGet = _itemData.Amount;
//			output += "Receved <color=red>" + amountGet + "</color>" + _itemData.itemType.ToString();
//			_itemData.GetItem();
//		}

//		Dialog.Setup().ShowConfirm("", output, () =>
//		{
//			Dialog.Setup().Hide();
//		});
//		GameUtils.RaiseMessage(GameEvent.OnSubscriptionUpdate.Instance);

//	}
//	#endregion


//}

//[Serializable]
//public class IAPObject
//{
//	public string IAPID;
//	public string IAPIDSale
//	{
//		get
//		{
//			return IAPID + "sale";
//		}

//	}
//	public ProductType IAPproductType = ProductType.Consumable;
//}

//public class FirebaseConfigKey
//{
//	public const string PercentHPWhenContinue = "PercentHPWhenContinue";
//	public const string DiamondWhenContinue = "DiamondWhenContinue";
//	public const string UndeadTimeWhenContinue = "UndeadTimeWhenContinue";
//	public const string SaleData = "SaleData";
//}