using Godot;

// using RisizableContainerChildType = Godot.BoxContainer;
using ResizableContainerChildType = Godot.PanelContainer;

[GlobalClass]
public partial class ResizableHContainer : HBoxContainer {
	private int _ColumnCount = 0;

	[Export]
	public int ColumnCount {
		get => _ColumnCount;
		set {
			_ColumnCount = value;

			RefreshColumns();
		}
	}

	public bool Resizing = false;
	public bool CanDrag = false;

	// TODO: Mark nullable.
	public ResizableContainerChildType LeftDraggingContainer = null;
	public ResizableContainerChildType RightDraggingContainer = null;

	// Need a default constructor for Godot to be able to make the Node.
	public ResizableHContainer() {
	}

	public override void _Process(double delta) {
		base._Process(delta);

		if (this.ColumnCount != this.GetChildContainers().Count) {
			RefreshColumns();
		}
	}

	public override void _GuiInput(InputEvent @event) {
		base._GuiInput(@event);

		if (@event is InputEventMouseButton inputEventMouse) {
			// If we click in an area that can be dragged, set that is resizing.
			if (this.CanDrag && inputEventMouse.Pressed && inputEventMouse.ButtonIndex == MouseButton.Left) {
				this.CanDrag = false;
				this.Resizing = true;

				// Set the child containers minimum size to their current size (so we can manipulate their minimum size).
				foreach (var child in this.GetChildContainers()) {
					child.CustomMinimumSize = child.GetGlobalRect().Size;
				}
			} else if (!inputEventMouse.Pressed && inputEventMouse.ButtonIndex == MouseButton.Left) {
				LeftDraggingContainer = null;
				RightDraggingContainer = null;

				this.Resizing = false;
			}
		} else if (@event is InputEventMouseMotion inputEventMouseMotion) {
			if (this.Resizing) {
				if (this.LeftDraggingContainer != null && this.RightDraggingContainer != null) {
					Vector2 leftDraggingContainerNewMinimumSize;
					Vector2 rightDraggingContainerNewMinimumSize;

					// Offset the minimum size by the relative horizontal movement.
					leftDraggingContainerNewMinimumSize = this.LeftDraggingContainer.CustomMinimumSize + new Vector2(inputEventMouseMotion.Relative.X, 0);
					rightDraggingContainerNewMinimumSize = this.RightDraggingContainer.CustomMinimumSize - new Vector2(inputEventMouseMotion.Relative.X, 0);

					// Ensure the new minimum size values are greater than both the containers minimum size. If they are not, do not change the containers size.
					if (leftDraggingContainerNewMinimumSize >= LeftDraggingContainer.GetMinimumSize() && rightDraggingContainerNewMinimumSize >= RightDraggingContainer.GetMinimumSize()) {
						this.LeftDraggingContainer.CustomMinimumSize = leftDraggingContainerNewMinimumSize;
						this.RightDraggingContainer.CustomMinimumSize = rightDraggingContainerNewMinimumSize;
					}
				} else {
					GD.PrintErr("Got null dragging container. Left: " + this.LeftDraggingContainer + ". Right: " + this.RightDraggingContainer + ".");
				}
			} else {
				// Detect if the current global mouse position is over any of the child containers global rect.
				bool overResizableContainerChildType = false;
				foreach (var child in this.GetChildContainers()) {
					if (child.GetGlobalRect().HasPoint(inputEventMouseMotion.GlobalPosition)) {
						overResizableContainerChildType = true;
						break;
					}
				}

				// If the mouse is not over any of the children...
				if (!overResizableContainerChildType) {
					var containerChildren = GetChildContainers();

					// Then it must be between two children. Iterate the children left to right, finding the one which is closest to the mouse position on the left side.
					for (int i = 0; i < containerChildren.Count; i++) {
						var child = containerChildren[i];
						if (child.GlobalPosition.X < inputEventMouseMotion.GlobalPosition.X) {
							this.LeftDraggingContainer = child;
						} else {
							break;
						}
					}

					// Iterate the children right to left, finding the one which is closest to the mouse position on the right side.
					for (int i = containerChildren.Count - 1; i >= 0; i--) {
						var child = containerChildren[i];
						if (child.GlobalPosition.X > inputEventMouseMotion.GlobalPosition.X) {
							this.RightDraggingContainer = child;
						} else {
							break;
						}
					}

					// Set the mouse cursor to the drag one and set that we can begin to drag.
					this.MouseDefaultCursorShape = CursorShape.Hsplit;

					this.CanDrag = true;
				} else {
					// ...else if we are over a child container, set the cursor to the default arrow and set that we cannot drag.
					this.MouseDefaultCursorShape = CursorShape.Arrow;
					this.CanDrag = false;
				}
			}
		}
	}

	public void RefreshColumns() {
		int currentColumns = this.GetChildContainers().Count;

		// If there are not enough container children, add new resizable children until it is equal to the set column count.
		while (currentColumns < this.ColumnCount) {
			var newResizableContainerChildType = new ResizableContainerChildType();
			newResizableContainerChildType.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			newResizableContainerChildType.SizeFlagsVertical = SizeFlags.ExpandFill;

			// Add the new element, marking is as internal.
			this.AddChild(newResizableContainerChildType, @internal: InternalMode.Front);

			currentColumns += 1;
		}

		// If there are too many container children, remove resizable children until it is equal to the set column count.
		while (currentColumns > this.ColumnCount) {
			var containerChildren = this.GetChildContainers();

			ResizableContainerChildType removeResizableContainerChild = null;
			for (int i = containerChildren.Count - 1; i >= 0; i--) {
				var child = containerChildren[i];
				if (child is ResizableContainerChildType resizableContainerChild) {
					removeResizableContainerChild = resizableContainerChild;
					break;
				}
			}

			if (removeResizableContainerChild != null) {
				removeResizableContainerChild.QueueFree();
			} else {
				break;
			}

			currentColumns -= 1;
		}
	}

	public Godot.Collections.Array<ResizableContainerChildType> GetChildContainers() {
		var returnArray = new Godot.Collections.Array<ResizableContainerChildType>();

		foreach (var child in this.GetChildren(includeInternal: true)) {
			if (child is ResizableContainerChildType resizableContainerChild) {
				returnArray.Add(resizableContainerChild);
			}
		}

		return returnArray;
	}

	public void ColorChildContainer(PanelContainer panelContainer, Color? color) {
		if (color != null && color.HasValue) {
			panelContainer.AddThemeStyleboxOverride("panel", new StyleBoxFlat() {
				BgColor = color.Value,
			});
		} else {
			panelContainer.RemoveThemeStyleboxOverride("panel");
		}
	}

	public void ColorChildContainerIndex(int index, Color? color) {
		ColorChildContainer(panelContainer: GetChildContainers()[index], color: color);
	}

	public void ColorAllChildContainers(Color? color) {
		for (int i = 0; i < this.ColumnCount; i++) {
			ColorChildContainerIndex(index: i, color: color);
		}
	}
}