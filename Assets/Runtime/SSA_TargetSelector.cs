using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SSA_TargetSelector : MonoBehaviour
{
    public LayerMask hitMask = ~0;
    public Color ringColor = new Color(1f, 0.85f, 0.2f, 1f);
    public float ringRadius = 0.6f;
    public int ringSegments = 64;
    public float ringWidth = 0.02f;

    SSA_TargetRing _currentRing;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f, hitMask, QueryTriggerInteraction.Ignore))
            {
                var unit = hit.collider.GetComponentInParent<SSA_UnitStats>();
                if (unit != null && unit.IsAlive)
                    Select(unit, hit.point);
            }
        }
    }

    void Select(SSA_UnitStats unit, Vector3 groundPoint)
    {
        if (_currentRing) Destroy(_currentRing.gameObject);
        var go = new GameObject("SSA_TargetRing");
        _currentRing = go.AddComponent<SSA_TargetRing>();
        _currentRing.Setup(unit, ringRadius, ringSegments, ringWidth, ringColor);
    }
}

public class SSA_TargetRing : MonoBehaviour
{
    LineRenderer lr;
    SSA_UnitStats unit;

    public void Setup(SSA_UnitStats u, float radius, int segments, float width, Color color)
    {
        unit = u;
        gameObject.transform.SetParent(u.SelectionAnchor ? u.SelectionAnchor : u.transform);
        gameObject.transform.localPosition = Vector3.zero;

        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = segments;
        lr.widthMultiplier = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(t) * radius, 0.02f, Mathf.Sin(t) * radius));
        }
    }
}
