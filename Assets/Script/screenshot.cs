using UnityEngine;
using System;

public class ScreenshotToPictures : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) // P basanda şəkil çəkəcək
        {
            // Windows-un Resimler (Pictures) qovluğunu tapır
            string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            // Fayl adı tarixi ilə
            string fileName = "screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

            // Tam yol
            string fullPath = System.IO.Path.Combine(picturesPath, fileName);

            // Screenshot yazılır
            ScreenCapture.CaptureScreenshot(fullPath);
            Debug.Log("Screenshot saved to: " + fullPath);
        }
    }
}
