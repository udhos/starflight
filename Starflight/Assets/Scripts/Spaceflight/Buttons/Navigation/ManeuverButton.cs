﻿
using UnityEngine;

public class ManeuverButton : ShipButton
{
	// if this is true we are transitioning to orbit or the docking bay
	bool m_isTransitioning;

	// what are we transitioning to?
	PD_General.Location m_nextLocation;

	// get the label for this button
	public override string GetLabel()
	{
		return "Maneuver";
	}

	public override bool Execute()
	{
		// get to the player data
		PlayerData playerData = DataController.m_instance.m_playerData;

		// where are we?
		switch ( playerData.m_general.m_location )
		{
			case PD_General.Location.DockingBay:

				// play the error sound
				SoundController.m_instance.PlaySound( SoundController.Sound.Error );

				// display the error message
				m_spaceflightController.m_messages.ChangeText( "<color=white>Standing by to launch.</color>" );

				// turn off the button light
				m_spaceflightController.m_buttonController.UpdateButtonSprites();

				// return false to not make the button active
				return false;

			case PD_General.Location.JustLaunched:

				Debug.Log( "Player is maneuvering - switching to the star system location." );

				// yes - switch to the star system location
				m_spaceflightController.SwitchLocation( PD_General.Location.StarSystem );

				break;

			case PD_General.Location.StarSystem:

				// show the system display
				m_spaceflightController.m_displayController.ChangeDisplay( m_spaceflightController.m_displayController.m_systemDisplay );

				break;

			case PD_General.Location.InOrbit:

				// fade the map to black
				m_spaceflightController.m_map.StartFade( 0.0f, 2.0f );

				// we are now transitioning
				m_isTransitioning = true;

				// transition to the star system
				m_nextLocation = PD_General.Location.StarSystem;

				// display message
				m_spaceflightController.m_messages.ChangeText( "<color=white>Leaving orbit...</color>" );

				break;
		}

		// reset the current speed
		playerData.m_general.m_currentSpeed = 0.0f;

		// return true to keep the button lit and active
		return true;
	}

	public override bool Update()
	{
		// get to the game data
		GameData gameData = DataController.m_instance.m_gameData;

		// get to the player data
		PlayerData playerData = DataController.m_instance.m_playerData;

		// are we currently transitioning?
		if ( m_isTransitioning )
		{
			// yes - get the current map fade amount
			float mapFadeAmount = m_spaceflightController.m_map.GetCurrentFadeAmount();

			// is it completely black yet?
			if ( mapFadeAmount == 0.0f )
			{
				// we are not transitioning any more
				m_isTransitioning = false;

				// which location do we want to switch to?
				switch ( m_nextLocation )
				{
					case PD_General.Location.DockingBay:

						Debug.Log( "Player exited maneuver while near starport - switching to the docking bay location." );

						// switch to the docking bay location
						m_spaceflightController.SwitchLocation( PD_General.Location.DockingBay );

						// play the docking bay door close animation
						m_spaceflightController.m_dockingBay.CloseDockingBayDoors();

						// turn off the maneuver function
						m_spaceflightController.m_buttonController.DeactivateButton();

						break;

					case PD_General.Location.InOrbit:

						Debug.Log( "Player exited maneuver while near a planet - switching to the in orbit location." );

						// switch to the in orbit location
						m_spaceflightController.SwitchLocation( PD_General.Location.InOrbit );

						// turn off the maneuver function
						m_spaceflightController.m_buttonController.DeactivateButton();

						break;

					case PD_General.Location.StarSystem:

						Debug.Log( "Player is breaking orbit - switching to the star system location." );

						// switch to the in orbit location
						m_spaceflightController.SwitchLocation( PD_General.Location.StarSystem );

						break;
				}
			}

			return true;
		}

		// check if we want to stop maneuvering
		if ( InputController.m_instance.SubmitWasPressed() )
		{
			// turn off the engines
			m_spaceflightController.m_player.TurnOffEngines();

			// are we in a star system?
			if ( playerData.m_general.m_location == PD_General.Location.StarSystem )
			{
				// do we have a planet to orbit?
				if ( m_spaceflightController.m_starSystem.m_planetToOrbitId == -1 )
				{
					// nope - just turn off the maneuver function
					m_spaceflightController.m_buttonController.DeactivateButton();
				}
				else
				{
					// yep - remember the planet
					playerData.m_general.m_currentPlanetId = m_spaceflightController.m_starSystem.m_planetToOrbitId;

					// fade the map to black
					m_spaceflightController.m_map.StartFade( 0.0f, 2.0f );

					// we are now transitioning
					m_isTransitioning = true;

					// play the activate sound
					SoundController.m_instance.PlaySound( SoundController.Sound.Activate );

					// is this arth?
					if ( m_spaceflightController.m_starSystem.m_planetToOrbitId == gameData.m_misc.m_arthPlanetId )
					{
						// yes - transition to the docking bay
						m_nextLocation = PD_General.Location.DockingBay;

						// display message
						m_spaceflightController.m_messages.ChangeText( "<color=white>Initiating docking procedure...</color>" );
					}
					else
					{
						// no - transition to in orbit
						m_nextLocation = PD_General.Location.InOrbit;

						// display message
						m_spaceflightController.m_messages.ChangeText( "<color=white>Initiating orbital maneuver...</color>" );
					}

					// stop here
					return true;
				}
			}
		}
		else
		{
			// get the controller stick position
			float x = InputController.m_instance.m_x;
			float z = InputController.m_instance.m_y;

			// create our 3d move vector from the controller position
			Vector3 moveVector = new Vector3( x, 0.0f, z );

			// check if the move vector will actually move the ship (that the controller is not centered)
			if ( moveVector.magnitude > 0.5f )
			{
				// normalize the move vector to a length of 1.0
				moveVector.Normalize();

				// update the direction
				playerData.m_general.m_currentDirection = Vector3.Slerp( playerData.m_general.m_currentDirection, moveVector, Time.deltaTime * 2.0f );

				// turn the engines on
				m_spaceflightController.m_player.TurnOnEngines();
			}
			else
			{
				// turn the engines off
				m_spaceflightController.m_player.TurnOffEngines();
			}
		}

		// returning true prevents the default spaceflight update from running
		return true;
	}
}
