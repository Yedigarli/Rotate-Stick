using UnityEngine;

public class SkinApplier : MonoBehaviour
{
    public enum ApplierLocation
    {
        Menu,
        Game,
    }

    public ApplierLocation location; // Inspector-da bunu seçəcəksiniz

    public Transform skinVisual;
    public SpriteRenderer skinRenderer;

    private SkinData[] skins;

    private void Awake()
    {
        skins = Resources.LoadAll<SkinData>("Skins");
        ApplySavedSkin();
    }

    public void ApplySkin(SkinData skin)
    {
        if (skin == null || skinRenderer == null)
            return;

        skinRenderer.sprite = skin.sprite;

        // Yerləşdiyi məkana görə fərqli nizamlamaları tətbiq et
        if (location == ApplierLocation.Menu)
        {
            skinVisual.localScale = skin.menuScale;
            skinVisual.localPosition = new Vector3(0, skin.menuYOffset, 0);
        }
        else if (location == ApplierLocation.Game)
        {
            skinVisual.localScale = skin.gameScale;
            skinVisual.localPosition = new Vector3(0, skin.gameYOffset, 0);
        }

        PlayerPrefs.SetString("SelectedSkin", skin.skinID);
    }

    void ApplySavedSkin()
    {
        if (skins == null || skins.Length == 0)
            return;

        string savedID = PlayerPrefs.GetString("SelectedSkin", "");

        foreach (var skin in skins)
        {
            if (skin.skinID == savedID)
            {
                ApplySkin(skin);
                return;
            }
        }
        ApplySkin(skins[0]);
    }
}
