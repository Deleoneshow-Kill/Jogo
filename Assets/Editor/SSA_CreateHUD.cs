using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SSA_CreateHUD
{
    [MenuItem("SSA/HUD/1) Criar HUD SSA (Canvas + Roda + Ordem)")]
    public static void CreateHUD()
    {
        var canvasGO = GameObject.Find("SSA_HUD") ?? new GameObject("SSA_HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

        var panel = new GameObject("TurnOrderPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        var rtp = (RectTransform)panel.transform;
        rtp.anchorMin = new Vector2(0, 0.5f); rtp.anchorMax = new Vector2(0, 0.5f);
        rtp.pivot = new Vector2(0, 0.5f); rtp.anchoredPosition = new Vector2(30, 0);
        rtp.sizeDelta = new Vector2(300, 400);
        panel.GetComponent<Image>().color = new Color(0,0,0,0.35f);

        var container = new GameObject("TurnOrderContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        container.transform.SetParent(panel.transform, false);
        var rtc = (RectTransform)container.transform;
        rtc.anchorMin = Vector2.zero; rtc.anchorMax = Vector2.one; rtc.offsetMin = new Vector2(10,10); rtc.offsetMax = new Vector2(-10,-10);

        var rowPrefab = new GameObject("TurnOrderRowPrefab", typeof(RectTransform), typeof(Text));
        var text = rowPrefab.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 20; text.alignment = TextAnchor.MiddleLeft; text.text = "1. Saga (SPD 150)";
        rowPrefab.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(rowPrefab, "Assets/SSA_Kit/TurnOrderRowPrefab.prefab");
        Object.DestroyImmediate(rowPrefab);

        var wheelGO = new GameObject("EnergyWheel", typeof(RectTransform), typeof(Image));
        wheelGO.transform.SetParent(canvasGO.transform, false);
        var rtw = (RectTransform)wheelGO.transform;
        rtw.anchorMin = new Vector2(1,0); rtw.anchorMax = new Vector2(1,0); rtw.pivot = new Vector2(1,0);
        rtw.anchoredPosition = new Vector2(-40, 40); rtw.sizeDelta = new Vector2(160,160);

        var img = wheelGO.GetComponent<Image>();
        img.sprite = Sprite.Create(MakeCircleTex(256), new Rect(0,0,256,256), new Vector2(0.5f,0.5f), 100);
        img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Radial360; img.fillAmount = 0f;

        var tm = Object.FindObjectOfType<SSA_TurnManager>();
        if (!tm) tm = new GameObject("SSA_TurnManager").AddComponent<SSA_TurnManager>();
        tm.TurnOrderContainer = rtc;
        tm.TurnOrderRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SSA_Kit/TurnOrderRowPrefab.prefab");
        tm.EnergyWheel = img;

        Selection.activeObject = canvasGO;
        Debug.Log("[SSA] HUD criado e ligado ao TurnManager.");
    }

    static Texture2D MakeCircleTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        var center = (size-1)/2f; var r = center*0.95f;
        for (int y=0; y<size; y++)
        for (int x=0; x<size; x++)
        {
            var dx = x - center; var dy = y - center;
            float d = Mathf.Sqrt(dx*dx + dy*dy);
            float a = Mathf.Clamp01(1f - Mathf.InverseLerp(r*0.98f, r, d));
            tex.SetPixel(x,y, new Color(1,1,1,a));
        }
        tex.Apply(false);
        return tex;
    }
}
