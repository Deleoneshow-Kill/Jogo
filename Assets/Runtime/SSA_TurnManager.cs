using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SSA_TurnManager : MonoBehaviour
{
    public List<SSA_UnitStats> Units = new List<SSA_UnitStats>();
    public Transform TurnOrderContainer;
    public GameObject TurnOrderRowPrefab;
    public Image EnergyWheel;
    public SSA_UnitStats CurrentUnit;

    readonly List<SSA_UnitStats> _orderPreview = new();

    void Start()
    {
        if (Units == null || Units.Count == 0)
            Units = FindObjectsOfType<SSA_UnitStats>().Where(u => u.IsAlive).ToList();

        RebuildOrderPreview();
        RefreshTurnOrderUI();
        if (Units.Count > 0) CurrentUnit = Units[0];
    }

    void Update()
    {
        if (CurrentUnit)
        {
            CurrentUnit.Energy = Mathf.Clamp(CurrentUnit.Energy + CurrentUnit.EnergyFillPerSecond * Time.deltaTime, 0, 100);
            if (EnergyWheel) EnergyWheel.fillAmount = CurrentUnit.Energy / 100f;

            if (CurrentUnit.Energy >= 100f)
            {
                CurrentUnit.Energy = 0;
                NextTurn();
            }
        }
    }

    public void NextTurn()
    {
        var firstAlive = _orderPreview.FirstOrDefault(u => u.IsAlive);
        if (firstAlive) CurrentUnit = firstAlive;

        if (_orderPreview.Count > 0)
        {
            var head = _orderPreview[0];
            _orderPreview.RemoveAt(0);
            _orderPreview.Add(head);
        }

        RefreshTurnOrderUI();
    }

    void RebuildOrderPreview()
    {
        _orderPreview.Clear();
        _orderPreview.AddRange(Units.Where(u => u.IsAlive).OrderByDescending(u => u.Speed));
    }

    void RefreshTurnOrderUI()
    {
        if (!TurnOrderContainer || !TurnOrderRowPrefab) return;

        foreach (Transform child in TurnOrderContainer) GameObject.Destroy(child.gameObject);

        for (int i = 0; i < _orderPreview.Count; i++)
        {
            var row = GameObject.Instantiate(TurnOrderRowPrefab, TurnOrderContainer);
            var txt = row.GetComponentInChildren<Text>();
            if (txt)
            {
                var u = _orderPreview[i];
                txt.text = $"{i+1}. {u.UnitName}  (SPD {u.Speed})";
                txt.color = (u == CurrentUnit) ? new Color(1f, 0.9f, 0.4f) : Color.white;
            }
            row.SetActive(true);
        }
    }
}
