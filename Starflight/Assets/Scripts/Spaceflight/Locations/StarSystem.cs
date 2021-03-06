﻿
using UnityEngine;

public class StarSystem : MonoBehaviour
{
	// our planet controllers
	public Planet[] m_planetController;

	// the nebula overlay
	public GameObject m_nebula;

	// convenient access to the spaceflight controller
	public SpaceflightController m_spaceflightController;

	// the shine script (so we can change the color)
	public Shine m_shine;

	// the id number of the planet we could be orbiting
	public int m_planetToOrbitId;

	// remember the current star
	GD_Star m_currentStar;

	// unity awake
	void Awake()
	{
	}

	// unity start
	void Start()
	{
	}

	// unity update
	void Update()
	{
		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// calculate the time to next flare (in FP days)
		var timeToFlare = m_currentStar.m_daysToNextFlare - playerData.m_general.m_gameTime;

		// did we flare already?
		if ( timeToFlare <= 0.0f )
		{
			// yes - the sun is stable again
			m_shine.SetSize( 128.0f, 129.0f );
		}
		else if ( timeToFlare <= 1.0f ) // are we flaring NOW?
		{
			// TODO: fuck up the player
		}
		else
		{
			var size = 1.0f / timeToFlare;

			var minSize = 128.0f + size * 64.0f;
			var maxSize = 129.0f + size * 128.0f;

			m_shine.SetSize( minSize, maxSize );

			//Debug.Log( "The star will flare in " + timeToFlare + " days - minSize = " + minSize + ", maxSize = " + maxSize );
		}

		// yes - did we just leave it?
		if ( Vector3.Magnitude( playerData.m_general.m_coordinates ) >= 8192.0f )
		{
			Debug.Log( "Player leaving the star system - switching to the hyperspace location." );

			// yes - calculate the position of the ship in hyperspace
			var newPosition = playerData.m_general.m_coordinates;

			newPosition.Normalize();

			newPosition *= m_currentStar.GetBreachDistance() + 16.0f;
			newPosition += m_currentStar.GetWorldCoordinates();

			// update the last hyperspace coordinates
			playerData.m_general.m_lastHyperspaceCoordinates = newPosition;

			// scale the speed of the player
			playerData.m_general.m_currentSpeed /= 4.0f;
			playerData.m_general.m_currentMaximumSpeed /= 4.0f;

			// switch modes now
			m_spaceflightController.SwitchLocation( PD_General.Location.Hyperspace );

			// don't do anything more here now
			return;
		}

		// get the nearest planet controller to the player
		var orbitPlanetController = GetNearestPlanetController();

		// did we get a planet controller?
		if ( orbitPlanetController != null )
		{
			// yes - get the distance of the player is to the planet
			var distanceToPlanet = orbitPlanetController.GetDistanceToPlayer();

			// are we close enough to orbit the planet?
			if ( distanceToPlanet <= orbitPlanetController.m_planetModel.transform.localScale.y )
			{
				// is this a different planet?
				if ( m_planetToOrbitId != orbitPlanetController.m_planet.m_id )
				{
					// yes - remember this planet
					m_planetToOrbitId = orbitPlanetController.m_planet.m_id;

					// let the player know
					m_spaceflightController.m_messages.ChangeText( "<color=white>Ship is within orbital range.</color>" );
				}
			}
			else
			{
				// no - forget this planet
				m_planetToOrbitId = -1;

				var spectralClass = m_currentStar.GetSpectralClass();

				// display the spectral class and ecosphere
				m_spaceflightController.m_messages.ChangeText( "<color=white>Stellar Parameters</color>\nSpectral Class: <color=white>" + m_currentStar.m_class + "</color>\nEcosphere: <color=white>" + spectralClass.m_ecosphereMin + " - " + spectralClass.m_ecosphereMax + "</color>" );
			}
		}

		// update encounters
		m_spaceflightController.UpdateEncounters();
	}

	// call this to initialize the star system before you show it
	public void Initialize()
	{
		// get to the game data
		var gameData = DataController.m_instance.m_gameData;

		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// did we change stars?
		if ( ( m_currentStar != null ) && ( m_currentStar.m_id == playerData.m_general.m_currentStarId ) )
		{
			// nope - don't do anything
			return;
		}

		// Debug.Log( "Initializing the star system with star ID " + playerData.m_starflight.m_currentStarId );

		// yes - so remember the current star
		m_currentStar = gameData.m_starList[ playerData.m_general.m_currentStarId ];

		// generate or load maps for each planet in this system
		for ( var i = 0; i < GD_Star.c_maxNumPlanets; i++ )
		{
			var planet = m_currentStar.GetPlanet( i );

			m_planetController[ i ].InitializePlanet( planet );
		}

		// update the system display
		m_spaceflightController.m_displayController.m_systemDisplay.ChangeSystem();
	}

	// call this to hide the starsystem objects
	public void Hide()
	{
		if ( !gameObject.activeInHierarchy )
		{
			return;
		}

		Debug.Log( "Hiding the star system location." );

		// hide the starsystem
		gameObject.SetActive( false );
	}

	// call this to show the starsystem objects
	public void Show()
	{
		if ( gameObject.activeInHierarchy )
		{
			return;
		}

		Debug.Log( "Showing the star system location." );

		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// show the starsystem
		gameObject.SetActive( true );

		// make sure the camera is at the right height above the zero plane
		m_spaceflightController.m_player.DollyCamera( 1024.0f );
		m_spaceflightController.m_player.SetClipPlanes( 512.0f, 1536.0f );

		// move the player object
		m_spaceflightController.m_player.transform.position = playerData.m_general.m_coordinates = playerData.m_general.m_lastStarSystemCoordinates;

		// calculate the new rotation of the player
		var newRotation = Quaternion.LookRotation( playerData.m_general.m_currentDirection, Vector3.up );

		// update the player rotation
		m_spaceflightController.m_player.m_ship.rotation = newRotation;

		// unfreeze the player
		m_spaceflightController.m_player.Unfreeze();

		// fade in the map
		m_spaceflightController.m_map.StartFade( 1.0f, 2.0f );

		// show / hide the nebula depending on if we are in one
		m_nebula.SetActive( m_currentStar.m_insideNebula );

		// show the system display
		m_spaceflightController.m_displayController.ChangeDisplay( m_spaceflightController.m_displayController.m_systemDisplay );

		// show the radar
		m_spaceflightController.m_radar.Show();

		// play the star system music track
		MusicController.m_instance.ChangeToTrack( MusicController.Track.StarSystem );

		// change the color of the sun
		Color color;

		switch ( m_currentStar.m_class )
		{
			case "M": color = new Color( 1.0f, 0.0f, 0.0f ); break;
			case "K": color = new Color( 1.0f, 0.4f, 0.0f ); break;
			case "G": color = new Color( 1.0f, 1.0f, 0.0f ); break;
			case "F": color = new Color( 1.0f, 1.0f, 1.0f ); break;
			case "A": color = new Color( 0.0f, 1.0f, 0.0f ); break;
			case "B": color = new Color( 0.4f, 0.4f, 1.0f ); break;
			case "O": color = new Color( 0.0f, 0.0f, 0.8f ); break;
			default: color = new Color( 1.0f, 0.5f, 1.0f ); break;
		}

		m_shine.SetColor( color );

		// turn off all the planets
		for ( var i = 0; i < GD_Star.c_maxNumPlanets; i++ )
		{
			m_planetController[ i ].DisablePlanet();
		}

		// turn on planets in this system
		var planetList = m_currentStar.GetPlanetList();

		foreach ( var planet in planetList )
		{
			if ( ( planet != null ) && ( planet.m_id != -1 ) )
			{
				m_planetController[ planet.m_orbitPosition ].EnablePlanet();
			}
		}
	}

	// find and return the planet controller that has the planet we are looking for
	public Planet GetPlanetController( int planetId )
	{
		foreach ( var planetController in m_planetController )
		{
			if ( planetController.m_planet != null )
			{
				if ( planetController.m_planet.m_id == planetId )
				{
					return planetController;
				}
			}
		}

		Debug.Log( "Warning - could not find planet " + planetId );

		return null;
	}

	// call this to get th nearest planet controller to the player
	public Planet GetNearestPlanetController()
	{
		float nearestDistanceToPlayer = 0.0f;

		Planet nearestPlanetController = null;

		foreach ( var planetController in m_planetController )
		{
			if ( planetController.m_planet != null )
			{
				float distanceToPlayer = planetController.GetDistanceToPlayer();

				if ( ( nearestPlanetController == null ) || ( distanceToPlayer < nearestDistanceToPlayer ) )
				{
					nearestDistanceToPlayer = distanceToPlayer;
					nearestPlanetController = planetController;
				}
			}
		}

		return nearestPlanetController;
	}

	// call this to get the id of the planet we could be orbiting around
	public int GetPlanetToOrbit()
	{
		return m_planetToOrbitId;
	}

	public bool GeneratingPlanets()
	{
		foreach ( var planetController in m_planetController )
		{
			if ( !planetController.MapsGenerated() )
			{
				return true;
			}
		}

		return false;
	}

	public float GeneratePlanets()
	{
		var totalProgress = 0.0f;

		foreach ( var planetController in m_planetController )
		{
			if ( !planetController.MapsGenerated() )
			{
				var progress = planetController.GenerateMaps();

				totalProgress += progress / 1.4f / 8.0f;
				break;
			}

			totalProgress += 1.0f / 8.0f;
		}

		return totalProgress;
	}
}
