﻿
public class StatusButton : ShipButton
{
	public override string GetLabel()
	{
		return "Status";
	}

	public override bool Execute()
	{
		// show the system display
		m_spaceflightController.m_displayController.ChangeDisplay( m_spaceflightController.m_displayController.m_statusDisplay );

		// remove the "active" dot from the current button
		m_spaceflightController.m_buttonController.UpdateButtonSprites();

		// play the update sound
		SoundController.m_instance.PlaySound( SoundController.Sound.Update );

		// this button has no update function
		return false;
	}
}
