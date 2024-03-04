using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackable
{
    public void OnDamaged(int damage, Vector3 pos);

    public bool OnHealed(int heal);
}
