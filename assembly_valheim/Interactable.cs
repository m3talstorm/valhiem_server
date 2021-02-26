using System;

public interface Interactable
{
	bool Interact(Humanoid user, bool hold);

	bool UseItem(Humanoid user, ItemDrop.ItemData item);
}
