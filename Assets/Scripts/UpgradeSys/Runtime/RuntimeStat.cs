using System;
using UnityEngine;

[Serializable]
public sealed class RuntimeStat
{
    [SerializeField] private EquipmentStat equipment = new();

    public EquipmentStat Equipment => equipment;
}
