﻿
using UnityEngine;

public class DockingBay : MonoBehaviour
{
	// the docking bay doors
	public Animator m_dockingBayDoorTop;
	public Animator m_dockingBayDoorBottom;

	// particle systems
	public ParticleSystem m_decompressionParticleSystem;

	// convenient access to the spaceflight controller
	public SpaceflightController m_spaceflightController;

	// the distance from the doors we want to park the ship
	float m_parkedPosition;

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
	}

	// call this to hide the docking bay
	public void Hide()
	{
		if ( !gameObject.activeInHierarchy )
		{
			return;
		}

		Debug.Log( "Hiding the docking bay location." );

		// hide the docking bay
		gameObject.SetActive( false );
	}

	// call this to switch to the docking bay
	public void Show()
	{
		if ( gameObject.activeInHierarchy )
		{
			return;
		}

		Debug.Log( "Showing the docking bay location." );

		// show the docking bay
		gameObject.SetActive( true );

		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// put us in the right spot for the docking bay launch sequence
		m_spaceflightController.m_player.transform.position = playerData.m_general.m_coordinates = new Vector3( 0.0f, 0.0f, 0.0f );

		// recalculate what the starting camera distance from the doors should be
		var verticalFieldOfView = m_spaceflightController.m_map.m_playerCamera.fieldOfView;
		var horizontalFieldOfView = 2.0f * Mathf.Atan( Mathf.Tan( verticalFieldOfView * Mathf.Deg2Rad * 0.5f ) * m_spaceflightController.m_map.m_playerCamera.aspect );
		var angle = Mathf.Deg2Rad * ( 180.0f - 90.0f - horizontalFieldOfView * Mathf.Rad2Deg * 0.5f );
		var tanAngle = Mathf.Tan( angle );
		var halfDoorWidth = 276.5f;

		m_parkedPosition = Mathf.Min( 1024.0f, halfDoorWidth * tanAngle );

		m_spaceflightController.m_player.DollyCamera( m_parkedPosition );
		m_spaceflightController.m_player.SetClipPlanes( 1.0f, 2048.0f );

		// freeze the player
		m_spaceflightController.m_player.Freeze();

		// reset the buttons
		m_spaceflightController.m_buttonController.RestoreBridgeButtons();

		// fade in the map
		m_spaceflightController.m_map.StartFade( 1.0f, 2.0f );

		// make sure we have the status display up
		m_spaceflightController.m_displayController.ChangeDisplay( m_spaceflightController.m_displayController.m_statusDisplay );

		// play the docking bay music track
		MusicController.m_instance.ChangeToTrack( MusicController.Track.DockingBay );
	}

	// call this to open the docking bay doors
	public void OpenDockingBayDoors()
	{
		// open the top docking bay door
		m_dockingBayDoorTop.Play( "Open" );

		// open the bottom docking bay door
		m_dockingBayDoorBottom.Play( "Open" );

		// fire up the particle system
		m_decompressionParticleSystem.Play();

		// play the docking bay door open sound
		SoundController.m_instance.PlaySound( SoundController.Sound.DockingBayDoorOpen );

		// play the decompression sound
		SoundController.m_instance.PlaySound( SoundController.Sound.Decompression );
	}

	// call this to close the docking bay doors
	public void CloseDockingBayDoors()
	{
		// open the top docking bay door
		m_dockingBayDoorTop.Play( "Close" );

		// open the bottom docking bay door
		m_dockingBayDoorBottom.Play( "Close" );

		// play the docking bay door open sound
		SoundController.m_instance.PlaySound( SoundController.Sound.DockingBayDoorClose );
	}

	// return the distance from the doors the ship is parked at
	public float GetParkedPosition()
	{
		return m_parkedPosition;
	}
}
