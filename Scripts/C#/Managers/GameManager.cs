using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Economy;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SceneName
{
    StartScene,
    LevelScene,
    GameScene,
    TempScene,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static ResourceManager Resource { get; private set; }

    public Action CompleteSceneLoad;

    private bool _bCompleteSceneLoad = false;

    [SerializeField]
    private Image _fade;
    [SerializeField]
    private Canvas _fadeCanvas;

    void Awake()
    {
        if (Instance == null)
            Init();

        else
            Destroy(gameObject);
    }

    private async void Init()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Resource = new ResourceManager();
        Resource.Init();
        Application.targetFrameRate = 60;
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await EconomyService.Instance.Configuration.SyncConfigurationAsync();
        CompleteSceneLoad += () => _bCompleteSceneLoad = true;

        // 아이템 데이터 생성
        foreach (ITEMNAME itemName in Enum.GetValues(typeof(ITEMNAME)))
            if ((int)itemName % 100 != 0)
            {
                Item.itemDataDic.TryAdd(itemName, EconomyService.Instance.Configuration.GetInventoryItem(itemName.ToString()).CustomDataDeserializable.GetAs<Storage.StorageItemData>());
            }
    }

    public async UniTask FadeAsync(float time, Action act = null)
    {
        _bCompleteSceneLoad = false;
        int temp = _fadeCanvas.sortingOrder;
        _fadeCanvas.sortingOrder = 1;
        _fade.gameObject.SetActive(true);
        float fadeTime = 0;

        while (fadeTime < 0.99f)
        {
            fadeTime += Time.deltaTime;
            _fade.color = Color.Lerp(Color.clear, Color.black, fadeTime);
            await UniTask.DelayFrame(1);
        }
        _fade.color = Color.black;

        await UniTask.Delay(TimeSpan.FromMilliseconds(500));

        act?.Invoke();

        await UniTask.WaitUntil(() => _bCompleteSceneLoad);

        fadeTime = 0;
        while (fadeTime < time)
        {
            _fade.color = Color.Lerp(Color.black, Color.clear, fadeTime / time);
            await UniTask.DelayFrame(1);
            fadeTime += Time.deltaTime;
        }
        _fade.gameObject.SetActive(false);
        _fadeCanvas.sortingOrder = temp;
    }

    public async UniTaskVoid LoadSceneAsync(SceneName next)
    {
        _fade.gameObject.SetActive(true);
        float fadeTime = 0;

        while (fadeTime < 0.99f)
        {
            _fade.color = Color.Lerp(Color.clear, Color.black, fadeTime);
            await UniTask.Delay(TimeSpan.FromMilliseconds(10));
            fadeTime += 0.0167f;
        }

        SceneManager.LoadScene((int)SceneName.TempScene);
        await UniTask.WaitUntil(() => SceneManager.GetActiveScene().buildIndex == (int)SceneName.TempScene);

        AsyncOperation ao = SceneManager.LoadSceneAsync((int)next);
        ao.allowSceneActivation = false;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        await UniTask.WhenAll(UniTask.WaitUntil(() => ao.progress >= 0.89), UniTask.Delay(TimeSpan.FromSeconds(1.5f)));
        ao.allowSceneActivation = true;
        await UniTask.WaitUntil(() => SceneManager.GetActiveScene().buildIndex == (int)next);

        //스테이지에 맞는 맵을 로드해야 함


        fadeTime = 0;
        while (fadeTime < 0.99f)
        {
            _fade.color = Color.Lerp(Color.black, Color.clear, fadeTime);
            await UniTask.Delay(TimeSpan.FromMilliseconds(10));
            fadeTime += 0.0334f;
        }
        _fade.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Resource.Clear();
    }
}
