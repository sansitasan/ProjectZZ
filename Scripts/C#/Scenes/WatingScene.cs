using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WatingScene : NetworkBehaviour
{
    [SerializeField] private GameObject StorageObj;
    [SerializeField] private GameObject StoreObj;

    public void StartGame()
    {
        FadeInClientRpc();
        GameManager.Instance.FadeAsync(1f, () => NetworkManager.Singleton.SceneManager.LoadScene("KS", LoadSceneMode.Single)).Forget();
    }

    private void ChangeScene()
    {
        ChangeSceneServerRpc();
    }

    [ServerRpc]
    private void ChangeSceneServerRpc()
    {
        FadeInClientRpc();
        GameManager.Instance.FadeAsync(1f, () => NetworkManager.Singleton.SceneManager.LoadScene("KS", LoadSceneMode.Single)).Forget();
    } 

    [ClientRpc]
    private void FadeInClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("In");
        if (!IsHost)
            GameManager.Instance.FadeAsync(1f).Forget();
    }

    public void TurnOnStorage()
    {
        StorageObj.SetActive(true);
    }

    public void TurnOnStore()
    {
        StoreObj.SetActive(true);
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainScene");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StoreObj.SetActive(false);
            StorageObj.SetActive(false);
        }

    }

    //private async void Test()
    //{
    //    var configAssignemntHash = EconomyService.Instance.Configuration.GetConfigAssignmentHash();

    //    try
    //    {
    //        var arguments = new IncrementBalanceParam("SCRAP", configAssignemntHash);
    //        var response = await CloudCodeService.Instance.CallModuleEndpointAsync<long>("StorageModule", "SayHello", new Dictionary<string, object> { { "param", arguments } });
    //        Debug.Log(response);
    //    }
    //    catch (CloudCodeException exception)
    //    {
    //        Debug.LogException(exception);
    //    }

    //}
}
