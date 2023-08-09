using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]

public class itemData : ScriptableObject
{
    //item data
    public char id;
    public string itemName;
    public Sprite icon;
    public GameObject prefab;
}