using Godot;

[GlobalClass]
public partial class CreatureControl : ResizableHContainer {
	// Need a default constructor for Godot to be able to make the Node.
	public CreatureControl() {
		this.ColumnCount = 5;
	}
}