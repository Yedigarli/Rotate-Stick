using UnityEngine;

public static class VibrationManager
{
    public static void LightVibration()
    {
        #if UNITY_ANDROID || UNITY_IOS
            // Android-də qısa titrəyiş simulyasiyası (Ciddi layihələrdə plugin məsləhətdir)
            Handheld.Vibrate(); 
        #endif
    }

    public static void HeavyVibration()
    {
        #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
        #endif
    }
}
