using UnityEngine;

public class PlayerPreview : MonoBehaviour
{
    [SerializeField]private PlayerAppearance appearance;
    [SerializeField]private Transform pivot;
    [SerializeField]private Camera previewCamera;
    [SerializeField]private bool artFacesright = false;
    [SerializeField]private bool faceRight = true;
    [SerializeField][Min(64)] private int textureSize = 512;

    private RenderTexture renderTexture;

    public RenderTexture Texture => renderTexture;

    private void Awake()
    {
        renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
        {
            name = "PlayerPreviewTexture"
        };
        renderTexture.Create();

        previewCamera.targetTexture = renderTexture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;

        bool flip = faceRight != artFacesright;
        pivot.localScale = new Vector3(flip ? -1f : 1f, 1f);
    }

    private void OnDestroy()
    {
        if (previewCamera != null)
            previewCamera.targetTexture = null;

        if(renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    public void SetVisible(bool visible)
    {
        previewCamera.enabled = visible;
    }

    public void SetWeapon(long? itemId)
    {
        appearance.SetWeapon(itemId);
        appearance.PlayIdle();
    }
    
    // 로딩창: 제자리 달리기
    public void PlayMove()
    {
        appearance.PlayMove();
    }
}
