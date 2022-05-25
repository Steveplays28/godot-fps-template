using System;
using System.Collections.Generic;
using Godot;

public class EquipmentManager : Node
{
	public Dictionary<int, Node> Equipment { get; private set; } = new Dictionary<int, Node>();
	public Node CurrentEquipment { get; private set; }

	public override void _Ready()
	{
		base._Ready();

		foreach (Node child in GetChildren())
		{
			Equipment.Add(Equipment.Count, child);
		}
	}

	public void Equip(int equipmentIndex)
	{
		// Check if anything is currently equipped
		if (CurrentEquipment != null)
		{
			// Unequip current equipment
			CurrentEquipment.Call("UnEquip");
			CurrentEquipment = null;
		}

		// Equip new equipment
		Equipment[equipmentIndex].Call("Equip");
		CurrentEquipment = Equipment[equipmentIndex];
	}

	public void UnEquip()
	{
		CurrentEquipment.Call("UnEquip");
		CurrentEquipment = null;
	}
}
