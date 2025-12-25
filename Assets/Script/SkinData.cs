using UnityEngine;

[CreateAssetMenu(fileName = "SkinData", menuName = "Skins/New Skin")]
public class SkinData : ScriptableObject
{
    public string skinID;
    public Sprite sprite;

    [Header("Menu Settings")]
    public Vector3 menuScale = Vector3.one;
    public float menuYOffset;

    [Header("Game Settings")]
    public Vector3 gameScale = Vector3.one;
    public float gameYOffset;

    [Header("Shop Settings")]
    public int price;
    public bool unlockedByDefault;
}
