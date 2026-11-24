using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    void Awake()
    {
        // Pixel-perfect camera: every art pixel becomes a whole number of screen pixels, so nothing shimmers or
        // wobbles while moving. 320x180 reference = 20 x 11 tiles visible (16 pixels = 1 unit).
        if (GetComponent<PixelPerfectCamera>() == null)
        {
            var ppc = gameObject.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 16;
            ppc.refResolutionX = 320;
            ppc.refResolutionY = 180;
            ppc.pixelSnapping = true;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        // Follow without any smoothing, and snap to the art pixel grid (1/16 unit). The character is drawn on the
        // same grid, so the two never disagree by one pixel (that disagreement looked like shaking, worst when
        // walking diagonally).
        const float pixel = 1f / 16f;
        float x = Mathf.Round(target.position.x / pixel) * pixel;
        float y = Mathf.Round(target.position.y / pixel) * pixel;
        transform.position = new Vector3(x, y, transform.position.z);
    }
}
