using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public struct EquipStat : INetworkSerializable
{
    public ITEMNAME ItemName;
    public int Durability;
    public int Armor;

    public EquipStat(ITEMNAME itemName, int durability = 0, int armor = 0)
    {
        ItemName = itemName;
        Durability = durability;
        Armor = armor;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Durability);
        serializer.SerializeValue(ref Armor);
    }

    public static EquipStat operator +(EquipStat a, EquipStat b)
    {
        a.Durability = a.Durability + b.Durability < 100 ? a.Durability + b.Durability : 100;
        a.Armor += b.Armor;
        return a;
    }
}

[Serializable]
public struct Stat : INetworkSerializable
{
    public int MaxHp;
    public int Hp;
    public float Speed;
    public int Gold;
    public int Damage;
    public int Range;
    public EquipStat HeadEquip;
    public EquipStat ClothEquip;

    public Stat(int maxHp = 0, int hp = 0, 
        float speed = 0, int gold = 0, int damage = 0, int range = 0, EquipStat head = new EquipStat(), EquipStat cloth = new EquipStat())
    {
        MaxHp = maxHp;
        Hp = hp;
        Speed = speed;
        Gold = gold;
        Damage = damage;
        Range = range;
        HeadEquip = head;
        ClothEquip = cloth;
    }

    public void SetHp(int damage)
    {
        Hp = Hp - damage > 0 ? Hp - damage : 0;
    }

    public void DestroyEquip(ITEMNAME item)
    {
        switch (item)
        {
            case var _ when item > ITEMNAME.SUBWEAPONEND && item < ITEMNAME.HEADEND:
                HeadEquip = new EquipStat();
                break;
            case var _ when item > ITEMNAME.HEADEND && item < ITEMNAME.CLOTHEND:
                ClothEquip = new EquipStat();
                break;
        };
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MaxHp);
        serializer.SerializeValue(ref Hp);
        serializer.SerializeValue(ref Speed);
        serializer.SerializeValue(ref Gold);
        serializer.SerializeValue(ref Damage);
        serializer.SerializeValue(ref Range);
        serializer.SerializeValue(ref HeadEquip);
        serializer.SerializeValue(ref ClothEquip);
    }

    public static Stat operator +(Stat a, Stat b)
    {
        a.Hp += b.Hp;
        if (a.Hp > a.MaxHp)
            a.Hp = a.MaxHp;
        a.Speed += b.Speed;
        a.Gold += b.Gold;
        a.Damage += b.Damage;
        a.Range += b.Range;
        return a;
    }
}