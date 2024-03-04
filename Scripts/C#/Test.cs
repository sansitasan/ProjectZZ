using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public List<TestMonsterController> _m;
    public Room _room;

    private void Start()
    {
        _room.Init();
        _m[0].Init(_room.monsterSpawners);
        //for (int i = 0; i < _m.Count; ++i)
        //    _m[i].Init(_room.monsterSpawners);
    }
}
