﻿
using UnityEngine;

public class InOrbit : MonoBehaviour
{
	// the nebula overlay
	public GameObject m_nebula;

	// the planet model
	public MeshRenderer m_planetModel;

	// the planet cloud
	public MeshRenderer m_planetClouds;

	// convenient access to the spaceflight controller
	public SpaceflightController m_spaceflightController;

	// current planet spin
	float m_spin;

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
		// slowly spin the planet
		m_spin += Time.deltaTime * 0.1f;

		// wrap the spin around to avoid FP issues
		if ( m_spin >= 360.0f )
		{
			m_spin -= 360.0f;
		}

		// calculate the new rotation quaternion
		var newRotation = Quaternion.Euler( 0.0f, 0.0f, m_spin );

		// apply it to the planet
		m_planetModel.transform.localRotation = newRotation;
	}

	// call this to hide the in orbit objects
	public void Hide()
	{
		if ( !gameObject.activeInHierarchy )
		{
			return;
		}

		Debug.Log( "Hiding the in orbit location." );

		// hide the starsystem
		gameObject.SetActive( false );
	}

	// call this to show the in orbit objects
	public void Show()
	{
		if ( gameObject.activeInHierarchy )
		{
			return;
		}

		Debug.Log( "Showing the in orbit location." );

		// get to the game data
		var gameData = DataController.m_instance.m_gameData;

		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// get to the star data
		var star = gameData.m_starList[ playerData.m_general.m_currentStarId ];

		// show the in orbit objects
		gameObject.SetActive( true );

		// get the planet controller
		var planetController = m_spaceflightController.m_starSystem.GetPlanetController( playerData.m_general.m_currentPlanetId );

		// set the scale of the planet model and clouds
		var scale = planetController.m_planet.GetScale();
		m_planetModel.transform.localScale = scale * 1.75f;
		m_planetClouds.transform.localScale = m_planetModel.transform.localScale * 1.01f;

		// move the player object
		m_spaceflightController.m_player.transform.position = playerData.m_general.m_coordinates = new Vector3( 0.0f, 0.0f, 0.0f );

		// make sure the camera dolly is the right distance
		m_spaceflightController.m_player.DollyCamera( 1024.0f );
		m_spaceflightController.m_player.SetClipPlanes( 512.0f, 1536.0f );

		// freeze the player
		m_spaceflightController.m_player.Freeze();

		// reset the buttons
		m_spaceflightController.m_buttonController.RestoreBridgeButtons();

		// fade in the map
		m_spaceflightController.m_map.StartFade( 1.0f, 2.0f );

		// show / hide the nebula depending on if we are in one
		m_nebula.SetActive( star.m_insideNebula );

		// play the docking bay music track
		MusicController.m_instance.ChangeToTrack( MusicController.Track.InOrbit );

		// let the player know we've established orbit
		m_spaceflightController.m_messages.ChangeText( "<color=white>Orbit established.</color>" );

		// set the position of the sun
		Vector4 sunPosition = new Vector4( -10000.0f, 5000.0f, 0.0f, 0.0f );

		m_planetModel.material.SetVector( "_SunPosition", sunPosition );
		m_planetClouds.material.SetVector( "_SunPosition", sunPosition );

		// set up the clouds
		planetController.SetupClouds( m_planetClouds );

		// apply the material to the planet model
		MaterialUpdated();
	}

	public void MaterialUpdated()
	{
		if ( gameObject.activeInHierarchy )
		{
			// get to the player data
			var playerData = DataController.m_instance.m_playerData;

			// get the planet controller
			var planetController = m_spaceflightController.m_starSystem.GetPlanetController( playerData.m_general.m_currentPlanetId );

			// apply the material to the planet model
			m_planetModel.material = planetController.GetMaterial();
		}
	}
}
