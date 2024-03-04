using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class Gun : NetworkBehaviour
{   
    public ref GunData GunData { get => ref _gunData; }
    // 추후에 따로 Gundata Struct를 만들어 네트워크로 관리할 수 있도록 개선
    private GunData _gunData; // 총의 모든 정보 보유

    [SerializeField] private GunCamera _cam; // 카메라
    [SerializeField] private Recoil _recoil; // 반동 담당

    [SerializeField] private Transform _effectparent; // 이펙트들 모아둘 부모 오브젝트
    [SerializeField] private TextMeshProUGUI _ammoleftText;

    [SerializeField] private Transform _muzzleTransform; // 총알이 생성되는 위치

    [SerializeField] public Attachment testatt1; // 테스트용 부착물

    //private Animator _animator;

    private PlayerStatusUI _statusUI;
    private CancellationTokenSource _cancellationTokenSource;
    private float _timeSinceLastShot;
    private bool _isaiming = false;

    //private void Start()
    //{
    //    _animator = GetComponent<Animator>();
    //    _effectparent = GameObject.Find("Effect").transform;
    //    _recoil = GameObject.Find("recoil").GetComponent<Recoil>();
    //    _ammoleftText = GameObject.Find("Ammo left").GetComponent<TextMeshProUGUI>();
    //    //_cam = GameObject.Find("FollowPlayerCam").GetComponent<GunCamera>();
    //    transform.LookAt(_cam.transform.position + (_cam.transform.forward * 30));
    //    Init();
    //}

    public override void OnNetworkSpawn()
    {
        

        if (IsOwner)
        {
            _cam = gameObject.transform.root.GetComponentInChildren<GunCamera>();
            Init();
        }

        //if (!IsOwner) _cam.DisableCamera(); // 내 플레이어 아니면 카메라 비활성화
    }

    public void UIInit(PlayerStatusUI ui)
    {
        _statusUI = ui;
    }

    private void Init()
    {
        //SetGunData(GunDataFactory.GetGunData(ITEMNAME.TESTASSAULTRIFLE)); // test

        //_animator = GetComponent<Animator>();
        _effectparent = GameObject.Find("Effect").transform;
        _recoil = GameObject.Find("recoil").GetComponent<Recoil>();
        _ammoleftText = GameObject.Find("Ammo left")?.GetComponent<TextMeshProUGUI>();
        transform.LookAt(_cam.transform.position + (_cam.transform.forward * 30));
        _cam.SetZoomSpeed(_gunData.zoomSpeed);

        //SetAnimatorTransitionDuration();
    }

    public void SetGunData(GunData gunData)
    {
        gunData.Init();
        _gunData = gunData;
    }


    /// <summary>
    /// 클라이언트가 재장전 시작 시 ServerRPC 호출.
    /// </summary>
    public void StartReload(Inventory inventory)
    {
        // 서버에서 리로드 관리
        if (_gunData.isReloading) return;
        _gunData.isReloading = true;
        ReloadServerRPC(_gunData, inventory.GetComponent<NetworkObject>());
    }

    /// <summary>
    /// 서버에서 재장전 실행
    /// </summary>
    [ServerRpc]
    private void ReloadServerRPC(GunData gunData, NetworkObjectReference networkObjectReference, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log(gunData.isReloading);

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams {TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }}
        };
        NetworkObject networkObj = networkObjectReference;
        var inventory = networkObj.GetComponent<Inventory>();

        int reloadAmount = CalculateReloadAmount(gunData, inventory);

        if(reloadAmount > 0)
        {
            Reload(gunData, reloadAmount, clientRpcParams).Forget();
        } else
        {
            gunData.isReloading = false;
            ReloadClientRPC(gunData, clientRpcParams);
        }        
    }

    /// <summary>
    /// 플레이어 인벤토리를 확인하여 재장전할 양 계산.
    /// </summary>
    private int CalculateReloadAmount(GunData gunData, Inventory inventory)
    {

        int reloadAmount = 0;
        int requiredAmmo = gunData.magSize - gunData.currentAmmo;

        while (reloadAmount < requiredAmmo)
        {
            if (inventory.HasItem((ITEMNAME)Enum.Parse(typeof(ITEMNAME), gunData.ammoType.ToString()), out InventoryItem item))
            {
                if (reloadAmount + item.currentCount < requiredAmmo)
                {
                    // 총알 아이템의 현재 갯수를 모두 소모해도 탄약이 더 필요하다면 선택된 총알 아이템 제거
                    reloadAmount += item.currentCount;
                    inventory.RemoveItem(item);
                }
                else
                {
                    int removedCount = requiredAmmo - reloadAmount;
                    item.currentCount -= removedCount;
                    reloadAmount = requiredAmmo;
                    inventory.items[inventory.FindIndex(item)] = item;
                }
            }
            else
            {
                Debug.Log("No ammo");
                break;
            }
        }

        return reloadAmount;
    }

    /// <summary>
    /// 재장전 (서버에서 실행)
    /// </summary>
    private async UniTask Reload(GunData gunData, int amount, ClientRpcParams clientRpcParams)
    {
        Debug.Log("Reload Start");
        await UniTask.Delay((int)(1000 * gunData.reloadTime));
        gunData.currentAmmo += amount;
        gunData.isReloading = false;
        Debug.Log("Reload finish");
        ReloadClientRPC(gunData, clientRpcParams);

    }

    /// <summary>
    /// 재장전 완료, 서버에서 계산된 gunData로 덮어씌움.
    /// </summary>
    [ClientRpc]
    private void ReloadClientRPC(GunData gundata, ClientRpcParams clientRpcParams)
    {
        _gunData = gundata;
    }

    //[ClientRpc]
    //private void ReloadClientRPC(int amount, ClientRpcParams clientRpcParams)
    //{
    //    _gunData.currentAmmo += amount;
    //}

    // 총 발사 가능 여부 판단
    private bool CanShoot() => !_gunData.isReloading && _gunData.currentAmmo > 0 && (_timeSinceLastShot > 1f / _gunData.fireRate || _gunData.isAutofire);


    /// <summary>
    /// 총의 실제 발사.
    /// </summary>
    public void Shoot()
    {   
        if (CanShoot())
        {   
            for(int i = 0; i < _gunData.bulletsPerShoot; i++)
            {
                Random.InitState(i + DateTime.Now.Millisecond);
                float spreadx = Random.Range(-_gunData.spreadRate, _gunData.spreadRate) / 5; // 탄퍼짐
                float spready = Random.Range(-_gunData.spreadRate, _gunData.spreadRate) / 5;
                float spreadz = Random.Range(-_gunData.spreadRate, _gunData.spreadRate) / 5;
                float distance = 0;
                Vector3 bulletDir;
                Ray ray = new(_cam.transform.position, _cam.transform.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, 1000))
                {   
                    bulletDir = hit.point - _muzzleTransform.position;
                    distance = Vector3.Distance(hit.point, _muzzleTransform.position);
                }
                else
                {
                    bulletDir = ray.GetPoint(1000) - _muzzleTransform.position;
                    distance = Vector3.Distance(ray.GetPoint(1000), _muzzleTransform.position);
                }
                distance = Mathf.Max(1, distance);

                bulletDir += new Vector3(distance * spreadx, distance * spready, distance * spreadz);

                if (IsServer)
                {
                    SpawnBulletServerRPC(bulletDir);
                }
                else
                {
                    SpawnClientBullet(bulletDir);
                }

            }

            _timeSinceLastShot = 0;
            _gunData.currentAmmo -= 1;
            _statusUI.ModifyBulletCount(_gunData.currentAmmo, _gunData.magSize);
            if (!_isaiming)                
                 _recoil.MakeRecoil(_gunData.recoilX, _gunData.recoilY, _gunData.recoilZ); // 반동 생성 
            else
                 _recoil.MakeRecoil(_gunData.aimRecoilX, _gunData.aimRecoilY, _gunData.aimRecoilZ);
                
        }
    }
    /// <summary>
    /// 클라이언트의 로컬 총알을 먼저 생성한 후, ServerRPC로 서버 총알 생성
    /// </summary>
    /// <param name="dir"></param>
    private void SpawnClientBullet(Vector3 dir)
    {
        //Debug.Log("SpawnClientBullet");
        GameObject bullet = BulletFactory.CreateBullet(BULLET_TYPE.Client, _muzzleTransform.position);
        bullet.GetComponent<ClientBullet>().Init(dir, _gunData.bulletSpeed, _gunData.bulletLifetime);
        SpawnBulletServerRPC(dir);
    }

    /// <summary>
    /// 서버 총알 생성하고, ClientRPC로 모든 클라이언트에 총알 생성 지시
    /// </summary>
    [ServerRpc]
    private void SpawnBulletServerRPC(Vector3 dir)
    {
        //Debug.Log("SpawnBulletServerRPC");
        GameObject bullet = BulletFactory.CreateBullet(BULLET_TYPE.Server, _muzzleTransform.position);
        bullet.GetComponent<ServerBullet>().Init(dir, _gunData.bulletSpeed, _gunData.bulletLifetime, _gunData.damage);
        bullet.GetComponent<NetworkObject>().Spawn();
        SpawnBulletClientRPC(dir);
    }

    /// <summary>
    /// 한 클라이언트가 총알을 쏠 시 다른 클라이언트들도 그 총알을 생성하도록 함
    /// </summary>
    /// <param name="dir"></param>
    [ClientRpc]
    private void SpawnBulletClientRPC(Vector3 dir)
    {
        if (IsOwner) return;
        //Debug.Log("SpawnBulletClientRPC");
        GameObject bullet = BulletFactory.CreateBullet(BULLET_TYPE.Client, _muzzleTransform.position);
        bullet.GetComponent<ClientBullet>().Init(dir, _gunData.bulletSpeed, _gunData.bulletLifetime);
    }


    /// <summary>
    /// 자동사격. 좌클릭 누르고있으면 계속 발사. 가능한 총과 불가능한 총이 있음. StartShoot()에서 호출.
    /// </summary>
    public async UniTask AutoFire(CancellationToken fireCancellationToken)
    {   
        while (true)
        {
            for (int i = 0; i < _gunData.bulletsPerShoot; i++)
            {
                Shoot();
            }
            await UniTask.Delay((int)(1000 / _gunData.fireRate), cancellationToken: fireCancellationToken); // 다음 발사까지 걸리는 시간

            if (fireCancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Start to shoot. gundata의 Autofire 여부에 따라, 좌클릭을 누르고 있으면 자동사격을 하거나 안함.
    /// </summary>
    public void StartShoot()
    {
        if (!IsOwner) return;
        if (_gunData.isAutofire)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken fireCancellationToken = _cancellationTokenSource.Token;

            UniTask.Void(async () =>
            {
                await AutoFire(fireCancellationToken);
            });
        }
        else
        {
            Shoot();
        }
        
    }
    
    /// <summary>
    /// Stop to shoot.
    /// </summary>
    public void StopShoot()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void Aim()
    {
        _isaiming = true;
        //_animator.SetBool("Aiming", true);
        //Debug.Log(_gunData.zoomRate);
        _cam.SetTargetFOV(_gunData.zoomRate);
    }

    public void StopAim()
    {
        _isaiming = false;
        //_animator.SetBool("Aiming", false);
        _cam.SetTargetFOV();
    }

    public void SetMuzzleTransform(Transform muzzle)
    {
        _muzzleTransform = muzzle;
    }


    private void Update()
    {
        if (IsOwner)
        {
            _timeSinceLastShot += Time.deltaTime;
            if(_ammoleftText) _ammoleftText.text = $"Ammo left: {_gunData.currentAmmo} / {_gunData.magSize} \nAmmoType: {_gunData.ammoType}";
            Debug.DrawRay(_cam.transform.position, _cam.transform.forward * 5, Color.red);
        }    
    }
    private void OnDisable()
    {
        _gunData.isReloading = false;
    }
}