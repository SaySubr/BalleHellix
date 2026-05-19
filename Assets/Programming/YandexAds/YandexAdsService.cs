#pragma warning disable 0414, 0649 //выключение предупреждений о неиспользуемых полях и полях, которые не присваиваются (они используются в инспекторе Unity)

using System;
using Config;
using MainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using YandexMobileAds;
using YandexMobileAds.Base;

public class YandexAdsService : MonoBehaviour
{
    //77 строка обьяснения логики получение рекламы.
    private const string ServiceObjectName = "YandexAdsService";

    [Header("Banner")]
    [SerializeField] private bool showBannerOnStart = true;
    [SerializeField] private string bannerAdUnitId = "R-M-19302950-1"; //Вставить ключ от баннера из Yandex partner https://partner.yandex.ru

    [Header("Interstitial")]
    [SerializeField] private string interstitialAdUnitId = "R-M-19302950-2"; //Вставить ключ от межстраничной рекламы из Yandex partner https://partner.yandex.ru
    [SerializeField] private int interstitialEveryLevelLaunches = 3;

    [Header("Rewarded")]
    [SerializeField] private string rewardedAdUnitId = "R-M-19302950-3";//Вставить ключ от рекламы за награду из Yandex partner https://partner.yandex.ru

    private static YandexAdsService instance;

    private Banner banner;
    private InterstitialAdLoader interstitialAdLoader;
    private Interstitial interstitial;
    private RewardedAdLoader rewardedAdLoader;
    private RewardedAd rewardedAd;

    private int levelLaunchesSinceInterstitial;
    private bool interstitialLoading;
    private bool rewardedLoading;
    private bool rewardedWasEarned;
    private Action rewardedCallback;
    private Action rewardedClosedWithoutRewardCallback;

    public static YandexAdsService Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static YandexAdsService EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<YandexAdsService>();
        if (instance != null)
            return instance;

        GameObject serviceObject = new GameObject(ServiceObjectName);
        instance = serviceObject.AddComponent<YandexAdsService>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

#if !UNITY_EDITOR
        interstitialAdLoader = new InterstitialAdLoader();
        rewardedAdLoader = new RewardedAdLoader();
#endif
        //YandexMobileAdsInterstitial - реклама, которая занимает весь экран и отображается в определенные моменты, например, между уровнями.
        //Она может быть показана после определенного количества запусков уровней или при других событиях в игре.
        //YandexMobileAdsBanner - реклама, которая отображается в виде баннера в определенной части экрана, например, внизу.
        //Она может быть показана на протяжении всего игрового процесса или в определенных сценах.
        //Снизу код содержит примеры того, как можно обойти жесткую привязку на уровне через события и показывать рекламу в нужные моменты,
        //а также как вызвать рекламу за награду в любом месте игры.
        //Прослушивание событий запуска уровней и загрузки сцен для показа рекламы в нужные моменты (в самих файлах нужного скрипта создать событие)
        GameLauncher.LevelLaunchRequested += HandleLevelLaunchRequested;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        //Пример вызова рекламы за награду:
        //YandexMobileAdsRewardedAd награду можно выдавать в любом месте, например, при нажатии на кнопку или после проигрыша на уровне.
        //Главное, чтобы был вызов метода ShowRewarded и передана логика выдачи награды в случае успешного просмотра рекламы.
        //Здесь пример жесткой привязки к кнопке, но лучше всего вызывать рекламу за награду через события, чтобы не создавать зависимость от конкретного UI элемента.
        //Код Пример, главная строка отвечающий за вызов рекламы за награду:
        // YandexAdsService.EnsureInstance().ShowRewarded(() => {...логика вызова рекламы}

        //Полный код вызова рекламы за награду:
        // private void OnRewardedReviveClicked()
        //{
        //    if (!gameOver || rewardedReviveUsed)
        //        return;

        //    StopRewardedReviveRoutine();
        //    ShowRewardedReviveButton(false);
        //    ShowRewardedReviveCountdown(false);

        //    YandexAdsService.EnsureInstance().ShowRewarded(
        //        ReviveAfterReward,
        //        ShowStandardGameOverButtonsAfterFailedReward);
        //}
    }

    private void Start()
    {
        if (showBannerOnStart)
            RequestBanner();

        RequestInterstitial();
        RequestRewarded();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
        
        GameLauncher.LevelLaunchRequested -= HandleLevelLaunchRequested;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        DestroyBanner();
        DestroyInterstitial();
        DestroyRewarded();
    }

    public void ShowRewarded(Action onRewarded, Action onClosedWithoutReward = null)
    {
#if UNITY_EDITOR
        Debug.Log("YandexAdsService: rewarded ad is simulated in editor.");
        onRewarded?.Invoke();
#else
        if (rewardedAd == null)
        {
            Debug.Log("YandexAdsService: rewarded ad is not ready.");
            RequestRewarded();
            onClosedWithoutReward?.Invoke();
            return;
        }

        rewardedCallback = onRewarded;
        rewardedClosedWithoutRewardCallback = onClosedWithoutReward;
        rewardedWasEarned = false;

        rewardedAd.OnAdClicked += HandleRewardedClicked;
        rewardedAd.OnAdShown += HandleRewardedShown;
        rewardedAd.OnAdFailedToShow += HandleRewardedFailedToShow;
        rewardedAd.OnAdImpression += HandleRewardedImpression;
        rewardedAd.OnAdDismissed += HandleRewardedDismissed;
        rewardedAd.OnRewarded += HandleRewarded;

        rewardedAd.Show();
#endif
    }

    public void ShowInterstitial()
    {
#if UNITY_EDITOR
        Debug.Log("YandexAdsService: interstitial ad is skipped in editor.");
#else
        if (interstitial == null)
        {
            Debug.Log("YandexAdsService: interstitial ad is not ready.");
            RequestInterstitial();
            return;
        }

        interstitial.OnAdClicked += HandleInterstitialClicked;
        interstitial.OnAdShown += HandleInterstitialShown;
        interstitial.OnAdFailedToShow += HandleInterstitialFailedToShow;
        interstitial.OnAdImpression += HandleInterstitialImpression;
        interstitial.OnAdDismissed += HandleInterstitialDismissed;

        interstitial.Show();
#endif
    }

    private void HandleLevelLaunchRequested(LevelData levelData)
    {
        levelLaunchesSinceInterstitial++;

        if (levelLaunchesSinceInterstitial < Mathf.Max(1, interstitialEveryLevelLaunches))
            return;

        levelLaunchesSinceInterstitial = 0;
        ShowInterstitial();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (showBannerOnStart && banner == null)
            RequestBanner();
    }

    private void RequestBanner()
    {
#if UNITY_EDITOR
        Debug.Log("YandexAdsService: banner ad is skipped in editor.");
#else
        DestroyBanner();

        BannerAdSize bannerSize = BannerAdSize.Sticky(GetScreenWidthDp());
        banner = new Banner(bannerSize, AdPosition.BottomCenter);

        banner.OnAdLoaded += HandleBannerLoaded;
        banner.OnAdFailedToLoad += HandleBannerFailedToLoad;
        banner.OnAdClicked += HandleBannerClicked;
        banner.OnImpression += HandleBannerImpression;
        banner.LoadAd(CreateAdRequest(bannerAdUnitId));
#endif
    }

    private void RequestInterstitial()
    {
#if !UNITY_EDITOR
        if (interstitialLoading || interstitial != null)
            return;

        if (interstitialAdLoader == null)
            interstitialAdLoader = new InterstitialAdLoader();

        interstitialLoading = true;
        interstitialAdLoader.LoadAd(
            CreateAdRequest(interstitialAdUnitId),
            onLoaded: HandleInterstitialLoaded,
            onFailed: HandleInterstitialFailedToLoad);
#endif
    }

    private void RequestRewarded()
    {
#if !UNITY_EDITOR
        if (rewardedLoading || rewardedAd != null)
            return;

        if (rewardedAdLoader == null)
            rewardedAdLoader = new RewardedAdLoader();

        rewardedLoading = true;
        rewardedAdLoader.LoadAd(
            CreateAdRequest(rewardedAdUnitId),
            onLoaded: HandleRewardedLoaded,
            onFailed: HandleRewardedFailedToLoad);
#endif
    }

    private int GetScreenWidthDp()
    {
        int screenWidth = (int)Screen.safeArea.width;
        return ScreenUtils.ConvertPixelsToDp(screenWidth);
    }

    private AdRequest CreateAdRequest(string adUnitId)
    {
        return new AdRequest(adUnitId);
    }

    private void DestroyBanner()
    {
#if !UNITY_EDITOR
        if (banner == null)
            return;

        banner.Destroy();
        banner = null;
#endif
    }

    private void DestroyInterstitial()
    {
#if !UNITY_EDITOR
        if (interstitial == null)
            return;

        interstitial.Destroy();
        interstitial = null;
#endif
    }

    private void DestroyRewarded()
    {
#if !UNITY_EDITOR
        if (rewardedAd == null)
            return;

        rewardedAd.Destroy();
        rewardedAd = null;
#endif
    }

    private void HandleBannerLoaded(object sender, EventArgs args)
    {
        banner?.Show();
    }

    private void HandleBannerFailedToLoad(object sender, AdFailureEventArgs args)
    {
        Debug.LogWarning("YandexAdsService: banner failed to load: " + args.Message);
    }

    private void HandleBannerClicked(object sender, EventArgs args)
    {
        Debug.Log("YandexAdsService: banner clicked.");
    }

    private void HandleBannerImpression(object sender, ImpressionData impressionData)
    {
        Debug.Log("YandexAdsService: banner impression.");
    }

    private void HandleInterstitialLoaded(Interstitial loadedInterstitial)
    {
        interstitialLoading = false;
        interstitial = loadedInterstitial;
    }

    private void HandleInterstitialFailedToLoad(AdFailedToLoadEventArgs args)
    {
        interstitialLoading = false;
        Debug.LogWarning("YandexAdsService: interstitial failed to load: " + args.Message);
    }

    private void HandleInterstitialClicked(object sender, EventArgs args)
    {
        Debug.Log("YandexAdsService: interstitial clicked.");
    }

    private void HandleInterstitialShown(object sender, EventArgs args)
    {
        Debug.Log("YandexAdsService: interstitial shown.");
    }

    private void HandleInterstitialFailedToShow(object sender, AdFailureEventArgs args)
    {
        Debug.LogWarning("YandexAdsService: interstitial failed to show: " + args.Message);
        CleanupInterstitialAfterShow();
    }

    private void HandleInterstitialImpression(object sender, ImpressionData impressionData)
    {
        Debug.Log("YandexAdsService: interstitial impression.");
    }

    private void HandleInterstitialDismissed(object sender, EventArgs args)
    {
        CleanupInterstitialAfterShow();
    }

    private void CleanupInterstitialAfterShow()
    {
        DestroyInterstitial();
        RequestInterstitial();
    }

    private void HandleRewardedLoaded(RewardedAd loadedRewardedAd)
    {
        rewardedLoading = false;
        rewardedAd = loadedRewardedAd;
    }

    private void HandleRewardedFailedToLoad(AdFailedToLoadEventArgs args)
    {
        rewardedLoading = false;
        Debug.LogWarning("YandexAdsService: rewarded failed to load: " + args.Message);
    }

    private void HandleRewardedClicked(object sender, EventArgs args)
    {
        Debug.Log("YandexAdsService: rewarded clicked.");
    }

    private void HandleRewardedShown(object sender, EventArgs args)
    {
        Debug.Log("YandexAdsService: rewarded shown.");
    }

    private void HandleRewardedFailedToShow(object sender, AdFailureEventArgs args)
    {
        Debug.LogWarning("YandexAdsService: rewarded failed to show: " + args.Message);
        CleanupRewardedAfterShow(false);
    }

    private void HandleRewardedImpression(object sender, ImpressionData impressionData)
    {
        Debug.Log("YandexAdsService: rewarded impression.");
    }

    private void HandleRewardedDismissed(object sender, EventArgs args)
    {
        CleanupRewardedAfterShow(rewardedWasEarned);
    }

    private void HandleRewarded(object sender, Reward reward)
    {
        rewardedWasEarned = true;
    }

    private void CleanupRewardedAfterShow(bool rewardEarned)
    {
        Action rewardAction = rewardedCallback;
        Action closedAction = rewardedClosedWithoutRewardCallback;

        rewardedCallback = null;
        rewardedClosedWithoutRewardCallback = null;
        rewardedWasEarned = false;

        DestroyRewarded();
        RequestRewarded();

        if (rewardEarned)
            rewardAction?.Invoke();
        else
            closedAction?.Invoke();
    }
}

#pragma warning restore 0414, 0649
