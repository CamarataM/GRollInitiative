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

	// Need a default constructor for Godot to be able to make the Node.
	public SpellSlotControl() {
		this.Label = new Label();
		this.SpinBox = new SpinBox();

		this.AddChild(this.Label);
		this.AddChild(this.SpinBox);
	}

	// Need a default constructor for Godot to be able to make the Node.
	public SpellSlotControl(int spellSlotIndex, int spellSlotAmount = 0) : this() {
		this.SpellSlotIndex = spellSlotIndex;
		this.SpinBox.Value = spellSlotAmount;
	}

	public void UpdateText() {
		this.Label.Text = this.SpellSlotIndex.ToString();
	}
}