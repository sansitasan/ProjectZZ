using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField]
    private Slider _hp;
    [SerializeField]
    private TextMeshProUGUI _bulletText;
    [SerializeField]
    private TextMeshProUGUI _hpText;

    public void Init(int hp, int maxHp, int bulletCount, int maxBulletCount)
    {
        _hp.maxValue = maxHp;
        _hp.value = hp;
        _hpText.text = $"{hp} / {maxHp}";
        _bulletText.text = $"{bulletCount} / {maxBulletCount}";
    }

    public void SetPlayerHP(int hp, int maxHp)
    {
        _hp.value = hp;
        _hpText.text = $"{hp} / {maxHp}";
    }

    public void ModifyBulletCount(int bulletCount, int maxBulletCount)
    {
        _bulletText.text = $"{bulletCount} / {maxBulletCount}";
    }
}
