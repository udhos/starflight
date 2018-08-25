﻿
public class LaunchButton : ShipButton
{
	private readonly ShipButton[] m_buttons = { new LaunchYesButton(), new LaunchNoButton() };

	public override string GetLabel()
	{
		return "Launch";
	}

	public override bool Execute()
	{
		if ( m_spaceflightController.m_inDockingBay )
		{
			m_spaceflightController.m_messages.text = "Confirm launch?";

			m_spaceflightController.m_buttonController.UpdateButtons( m_buttons );

			return true;
		}

		return false;
	}
}
