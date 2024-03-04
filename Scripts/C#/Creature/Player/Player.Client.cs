using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum PlayerInputs
{
    None,
    Idle,
    Move,
    Attack,
    StopAttack,
    Reload,
    Aim,
    StopAim,
    Back
}

public partial class Player : NetworkBehaviour, IAttackable
{
    private void InitOwner()
    {
        _mainCam.gameObject.SetActive(true);
        //_followPlayerCam.Follow = _headTransform;
        gameObject.layer = 9;
        transform.GetChild(1).gameObject.layer = 9;
        _interact.gameObject.SetActive(true);
        _interact.Init(this, _followPlayerCam.transform);
        _playerController = Util.GetOrAddComponent<PlayerController>(gameObject);
        _playerController.Init(_followPlayerCam, _iaa, _interact);
        _rotationTransform = transform.GetChild(0).GetChild(0).GetChild(0);
        transform.GetChild(0).GetChild(1).gameObject.layer = 9;

        _screenMid.x = Screen.width >> 1;
        _screenMid.y = Screen.height >> 1;
        Gun g = GetComponentInChildren<Gun>();
        g.UIInit(_statusUI);
        Util.SetGameLayerRecursive(g.gameObject, 6);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnOffSettingUI(true);
    }

    [ClientRpc]
    private void UIUpdateClientRpc(Stat stat, ClientRpcParams clientRpc = default)
    {
        _statusUI.SetPlayerHP(stat.Hp, stat.MaxHp);
    }

    private void InitAll()
    {
        _anim = GetComponentInChildren<Animator>();
        Gun g = GetComponentInChildren<Gun>();
        _gunTransform = g.gameObject.transform;
        Equip(GunDataFactory.GetGunData(ITEMNAME.ASSAULTRIFLE));
    }

    private void RotateMouse()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * _sensitive;
        float mouseY = Input.GetAxisRaw("Mouse Y") * _sensitive;

        _rotationX += mouseX;
        _rotationY = Mathf.Clamp(_rotationY + mouseY, _minX, _maxX);

        _followPlayerCam.transform.eulerAngles = new Vector3(-_rotationY, _followPlayerCam.transform.eulerAngles.y, 0);
        transform.eulerAngles = new Vector3(0, _rotationX, 0);

        Vector3 cross = Vector3.Cross(_targetOriginAngle.position - transform.position, Vector3.up);

        Vector3 value = Quaternion.AngleAxis(_rotationY, cross) * (_targetOriginAngle.position - transform.position) + transform.position;
        _target.position = value;
        _target.localEulerAngles = new Vector3(-_rotationY, 0, 0);
        value = Quaternion.AngleAxis(_rotationX, Vector3.up) * (_targetOriginAngle.position - transform.position) + transform.position;
        _rootTarget.position = value;
    }

    public void OnOffSettingUI(bool bOpen)
    {
        if (bOpen)
            Cursor.lockState = CursorLockMode.None;
        else
            Cursor.lockState = CursorLockMode.Locked;

        Cursor.visible = bOpen;
        enabled = !bOpen;
    }

    public void SetColor()
    {
        _skinnedMeshRenderer.materials[1].color = Color.white;
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            MoveCharacterServerRpc(_playerController.MoveDir);
            //RotateMouse();
        }
    }

    [ClientRpc]
    private void InputClientRpc(PlayerInputs pi = PlayerInputs.None, ClientRpcParams clientRpcParams = default)
    {
        if (pi != PlayerInputs.None)
        {
            _anim.SetBool(PlayerInputs.Move.ToString(), false);
            _anim.SetBool(PlayerInputs.Back.ToString(), false);
            //_anim.SetBool(pi.ToString(), true);
        }
    }

    [ClientRpc]
    private void InputClientRpc(Vector3 dir, PlayerInputs pi = PlayerInputs.None, ClientRpcParams clientRpcParams = default)
    {
        _rigidbody.velocity = dir;
        //_rigidbody.AddForce(-9.81f * Vector3.up, ForceMode.Acceleration);

        if (dir == Vector3.zero)
        {
            _anim.SetBool(PlayerInputs.Move.ToString(), false);
        }
        else
        {
            _anim.SetBool(PlayerInputs.Move.ToString(), true);
            if (Vector3.Dot(transform.forward, dir) >= 0)
                _anim.SetBool(PlayerInputs.Back.ToString(), false);
            else
                _anim.SetBool(PlayerInputs.Back.ToString(), true);
        }
    }
}
