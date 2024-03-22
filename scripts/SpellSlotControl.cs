using Godot;

public partial class SpellSlotControl : HBoxContainer {
	[Export]
	public Label Label;

	[Export]
	public SpinBox SpinBox;

	private int _SpellSlotIndex = -1;
	[Export]
	public int SpellSlotIndex {
		get => _SpellSlotIndex;
		set {
			_SpellSlotIndex = value;

			UpdateText();
		}
	}

	public CreatureControl.SpellSlotData LinkedSpellSlotData;

	// Need a default constructor for Godot to be able to make the Node.
	public SpellSlotControl() {
		this.Label = new Label();
		this.SpinBox = new SpinBox();
		this.SpinBox.Changed += () => {
			if (this.LinkedSpellSlotData != null) {
				this.LinkedSpellSlotData.SpellSlotAmount = (int) SpinBox.Value;
			}
		};

		this.AddChild(this.Label);
		this.AddChild(this.SpinBox);
	}

	// Need a default constructor for Godot to be able to make the Node.
	public SpellSlotControl(CreatureControl.SpellSlotData linkedSpellSlotData) : this() {
		this.LinkedSpellSlotData = linkedSpellSlotData;

		if (this.LinkedSpellSlotData != null) {
			this.SpellSlotIndex = linkedSpellSlotData.SpellSlotIndex;
			this.SpinBox.Value = linkedSpellSlotData.SpellSlotAmount;
		}
	}

	public void UpdateText() {
		this.Label.Text = this.SpellSlotIndex.ToString();
	}
}