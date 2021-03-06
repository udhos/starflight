﻿
using UnityEngine;

public class Encounter : MonoBehaviour
{
	// convenient access to the spaceflight controller
	public SpaceflightController m_spaceflightController;

	// the speed the alien ships move at
	public float m_alienShipSpeed;

	// the rate the alien ships turn at
	public float m_alienShipTurnRate;

	// the camera dolly speed
	public float m_cameraDollySpeed;

	// how often to update the target coordinates
	public float m_targetCoordinateUpdateFrequency;

	// alien ship models (need 8)
	public GameObject[] m_alienShipModelList;

	// template models that we will clone as needed (need 23)
	public GameObject[] m_alienShipModelTemplate;

	// the current encounter data (both player and game)
	public PD_Encounter m_pdEncounter;
	public GD_Encounter m_gdEncounter;

	// alien ship data
	PD_AlienShip[] m_alienShipList;

	// current dolly distance
	float m_currentDollyDistance;

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
		// get to the game data
		var gameData = DataController.m_instance.m_gameData;

		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// update the position and rotation of the active alien ship models
		for ( var alienShipIndex = 0; alienShipIndex < m_alienShipModelList.Length; alienShipIndex++ )
		{
			var alienShipModel = m_alienShipModelList[ alienShipIndex ];

			if ( alienShipModel.activeInHierarchy )
			{
				var alienShip = m_alienShipList[ alienShipIndex ];

				var encounterType = gameData.m_encounterTypeList[ alienShip.m_encounterTypeId ];

				switch ( encounterType.m_scriptId )
				{
					case 3:
						MechanUpdate( alienShip, alienShipModel );
						break;

					default:
						DefaultUpdate( alienShip, alienShipModel );
						break;
				}
			}
		}

		// finalize alien ships and camera transform
		FinalizeUpdate();

		// has the player left the encounter?
		if ( playerData.m_general.m_coordinates.magnitude >= 4096.0f )
		{
			// calculate the normalized exit direction vector
			var exitDirection = Vector3.Normalize( playerData.m_general.m_coordinates );

			// was the last location in hyperspace?
			if ( playerData.m_general.m_lastLocation == PD_General.Location.Hyperspace )
			{
				// yes - update the last hyperspace coordinates
				playerData.m_general.m_lastHyperspaceCoordinates += exitDirection * m_spaceflightController.m_encounterRange * 1.25f;
			}
			else 
			{
				// no - update the last star system coordinates
				playerData.m_general.m_lastStarSystemCoordinates += exitDirection * m_spaceflightController.m_encounterRange * 1.25f;
			}

			// yes - switch back to the last location
			m_spaceflightController.SwitchLocation( playerData.m_general.m_lastLocation );
		}
	}

	// call this to hide the encounter stuff
	public void Hide()
	{
		if ( !gameObject.activeInHierarchy )
		{
			return;
		}

		Debug.Log( "Hiding the encounter location." );

		// hide the hyperspace objects
		gameObject.SetActive( false );
	}

	// call this to show the encounter stuff
	public void Show()
	{
		if ( gameObject.activeInHierarchy )
		{
			return;
		}

		Debug.Log( "Showing the encounter location." );

		// show the hyperspace objects
		gameObject.SetActive( true );

		// show the player (ship)
		m_spaceflightController.m_player.Show();

		// make sure the camera is at the right height above the zero plane
		m_currentDollyDistance = 1024.0f;

		m_spaceflightController.m_player.DollyCamera( m_currentDollyDistance );
		m_spaceflightController.m_player.SetClipPlanes( 512.0f, 1536.0f );

		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// move the ship to where we are in the encounter
		m_spaceflightController.m_player.transform.position = playerData.m_general.m_coordinates = playerData.m_general.m_lastEncounterCoordinates;

		// calculate the new rotation of the player
		var newRotation = Quaternion.LookRotation( playerData.m_general.m_currentDirection, Vector3.up );

		// update the player rotation
		m_spaceflightController.m_player.m_ship.rotation = newRotation;

		// unfreeze the player
		m_spaceflightController.m_player.Unfreeze();

		// fade in the map
		m_spaceflightController.m_map.StartFade( 1.0f, 2.0f );

		// show the status display
		m_spaceflightController.m_displayController.ChangeDisplay( m_spaceflightController.m_displayController.m_statusDisplay );

		// reset the encounter
		Reset();

		// add the alien ships to the encounter
		AddAlienShips( true );

		// center the encounter coordinates on the player
		m_pdEncounter.m_currentCoordinates = playerData.m_general.m_coordinates;

		// play the star system music track
		MusicController.m_instance.ChangeToTrack( MusicController.Track.Encounter );

		// play the alarm sound
		SoundController.m_instance.PlaySound( SoundController.Sound.Alarm );
	}

	void Reset()
	{
		// get to the game data
		var gameData = DataController.m_instance.m_gameData;

		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// get the current encounter id
		var encounterId = playerData.m_general.m_currentEncounterId;

		// get to the encounter game data
		m_gdEncounter = gameData.m_encounterList[ encounterId ];

		// find the encounter in the player data (the list is continually sorted by distance so we have to search)
		foreach ( var encounter in playerData.m_encounterList )
		{
			if ( encounter.m_encounterId == encounterId )
			{
				m_pdEncounter = encounter;
				break;
			}
		}

		// get to the list of alien ships
		var alienShipList = m_pdEncounter.GetAlienShipList();

		// reset all of the alien ships
		foreach ( var alienShip in alienShipList )
		{
			// this alien ship has not been added yet
			alienShip.m_addedToEncounter = false;
		}

		// inactivate all of the alien ship models
		foreach ( var alienShip in m_alienShipModelList )
		{
			alienShip.SetActive( false );
		}

		// allocate array for alien ship list
		m_alienShipList = new PD_AlienShip[ m_alienShipModelList.Length ];
	}

	// adds a number of alien ships to the encounter - up to the maximum allowed by the encounter
	void AddAlienShips( bool justEnteredEncounter )
	{
		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// get to the list of alien ships
		var alienShipList = m_pdEncounter.GetAlienShipList();

		// go through alien ship slots (up to the maximum allowed at once)
		for ( var alienShipIndex = 0; alienShipIndex < m_gdEncounter.m_maxNumShipsAtOnce; alienShipIndex++ )
		{
			var alienShipModel = m_alienShipModelList[ alienShipIndex ];

			// is this slot active right now?
			if ( alienShipModel.activeInHierarchy )
			{
				// yes - skip it
				continue;
			}

			// no - go through all of the alien ships in the encounter and add the next one
			foreach ( var alienShip in alienShipList )
			{
				// has this alien ship already been added to the encounter?
				if ( alienShip.m_addedToEncounter )
				{
					// yes - skip it
					continue;
				}

				// is this alien ship dead?
				if ( alienShip.m_isDead )
				{
					// yes - skip it
					continue;
				}

				// generate a random position inside of a unit circle
				var randomPosition = Random.insideUnitCircle;

				Vector3 coordinates;

				if ( justEnteredEncounter )
				{
					// put alien ship in area approximately in the correct direction of approach
					coordinates = new Vector3( randomPosition.x, 0.0f, randomPosition.y ) * 256.0f + Vector3.Normalize( m_pdEncounter.m_currentCoordinates - playerData.m_general.m_lastHyperspaceCoordinates ) * 4096.0f;
				}
				else
				{
					// put alien ship in a random position on a circle around the player
					coordinates = Vector3.Normalize( new Vector3( randomPosition.x, 0.0f, randomPosition.y ) ) * 4096.0f;
				}

				// make alien ship face the center of the encounter space
				var direction = -Vector3.Normalize( coordinates );

				// update the alien ship
				alienShip.m_coordinates = coordinates;
				alienShip.m_targetCoordinates = Vector3.zero;
				alienShip.m_currentDirection = direction;
				alienShip.m_lastDirection = direction;
				alienShip.m_currentBankingAngle = 0.0f;
				alienShip.m_timeSinceLastTargetCoordinateChange = alienShipIndex / m_gdEncounter.m_maxNumShipsAtOnce * m_targetCoordinateUpdateFrequency;
				alienShip.m_addedToEncounter = true;

				// remove old model
				Tools.DestroyChildrenOf( alienShipModel );

				// reset the transform of the model
				alienShipModel.transform.SetPositionAndRotation( Vector3.zero, Quaternion.identity );

				// clone the model
				var alienShipModelTemplate = m_alienShipModelTemplate[ alienShip.m_encounterTypeId ];

				Instantiate( alienShipModelTemplate, alienShipModelTemplate.transform.localPosition, alienShipModelTemplate.transform.localRotation, alienShipModel.transform );

				// show the model
				alienShipModel.SetActive( true );

				// remember the alien ship associated with this model
				m_alienShipList[ alienShipIndex ] = alienShip;

				// we are done adding an alien ship to this slot
				break;
			}
		}
	}

	void MechanUpdate( PD_AlienShip alienShip, GameObject alienShipModel )
	{
		BuzzPlayer( alienShip, alienShipModel, 1.0f );
	}

	void DefaultUpdate( PD_AlienShip alienShip, GameObject alienShipModel )
	{
		BuzzPlayer( alienShip, alienShipModel, 1.0f );
	}

	void BuzzPlayer( PD_AlienShip alienShip, GameObject alienShipModel, float alienShipSpeedMultiplier )
	{
		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		MoveAlienShip( alienShip, alienShipModel, alienShipSpeedMultiplier, playerData.m_general.m_coordinates );
	}

	void MoveAlienShip( PD_AlienShip alienShip, GameObject alienShipModel, float alienShipSpeedMultiplier, Vector3 targetCoordinates )
	{
		// update time since we changed target coordinates for this alien ship
		alienShip.m_timeSinceLastTargetCoordinateChange += Time.deltaTime;

		// change target coordinates every so often
		if ( alienShip.m_timeSinceLastTargetCoordinateChange >= m_targetCoordinateUpdateFrequency )
		{
			var randomCoordinates = Random.insideUnitCircle;

			alienShip.m_targetCoordinates = targetCoordinates + Vector3.Normalize( new Vector3( randomCoordinates.x, 0.0f, randomCoordinates.y ) ) * 256.0f;

			alienShip.m_timeSinceLastTargetCoordinateChange -= m_targetCoordinateUpdateFrequency;
		}

		// steer the alien ship towards the target coordinates
		var desiredDirection = Vector3.Normalize( alienShip.m_targetCoordinates - alienShip.m_coordinates );

		alienShip.m_currentDirection = Vector3.Slerp( alienShip.m_currentDirection, desiredDirection, Time.deltaTime * m_alienShipTurnRate );

		// move the alien ship forward
		alienShip.m_coordinates += alienShip.m_currentDirection * Time.deltaTime * m_alienShipSpeed * alienShipSpeedMultiplier;
	}

	void FinalizeUpdate()
	{
		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// remember the extents
		var xExtent = 0.0f;
		var zExtent = 0.0f;

		// update the position and rotation of the active alien ship models
		for ( var alienShipIndex = 0; alienShipIndex < m_alienShipModelList.Length; alienShipIndex++ )
		{
			var alienShipModel = m_alienShipModelList[ alienShipIndex ];

			if ( alienShipModel.activeInHierarchy )
			{
				// get to the alien ship
				var alienShip = m_alienShipList[ alienShipIndex ];

				// set the rotation of the ship
				alienShipModel.transform.rotation = Quaternion.LookRotation( alienShip.m_currentDirection, Vector3.up );

				// get the number of degrees we are turning the ship (compared to the last frame)
				var bankingAngle = Vector3.SignedAngle( alienShip.m_currentDirection, alienShip.m_lastDirection, Vector3.up );

				// scale the angle enough so we actually see the ship banking (but max it out at 60 degrees in either direction)
				bankingAngle = Mathf.Max( -60.0f, Mathf.Min( 60.0f, bankingAngle * 48.0f ) );

				// interpolate towards the new banking angle
				alienShip.m_currentBankingAngle = Mathf.Lerp( alienShip.m_currentBankingAngle, bankingAngle, Time.deltaTime );

				// save the last direction
				alienShip.m_lastDirection = alienShip.m_currentDirection;

				// bank the ship based on the calculated angle
				alienShipModel.transform.rotation = Quaternion.AngleAxis( alienShip.m_currentBankingAngle, alienShip.m_currentDirection ) * alienShipModel.transform.rotation;

				// set the position of the ship
				alienShipModel.transform.position = alienShip.m_coordinates;

				// figure out how far away from the player this alien ship is
				var playerToShip = alienShip.m_coordinates - playerData.m_general.m_coordinates;

				xExtent = Mathf.Max( xExtent, Mathf.Abs( playerToShip.x ) );
				zExtent = Mathf.Max( zExtent, Mathf.Abs( playerToShip.z ) );
			}
		}

		// add some space around the extents
		xExtent += 192.0f;
		zExtent += 192.0f;

		// recalculate what the camera distance from the zero plane should be
		var verticalFieldOfView = m_spaceflightController.m_map.m_playerCamera.fieldOfView * Mathf.Deg2Rad;
		var horizontalFieldOfView = 2.0f * Mathf.Atan( Mathf.Tan( verticalFieldOfView * 0.5f ) * m_spaceflightController.m_map.m_playerCamera.aspect );
		var horizontalAngle = Mathf.Deg2Rad * ( 180.0f - 90.0f - horizontalFieldOfView * Mathf.Rad2Deg * 0.5f );
		var verticalAngle = Mathf.Deg2Rad * ( 180.0f - 90.0f - verticalFieldOfView * Mathf.Rad2Deg * 0.5f );
		var tanHorizontalAngle = Mathf.Tan( horizontalAngle );
		var tanVerticalAngle = Mathf.Tan( verticalAngle );

		var targetDollyDistance = Mathf.Max( xExtent * tanHorizontalAngle, zExtent * tanVerticalAngle, 1024.0f );

		// slowly dolly the camera
		m_currentDollyDistance = Mathf.Lerp( m_currentDollyDistance, targetDollyDistance, Time.deltaTime * m_cameraDollySpeed );

		m_spaceflightController.m_player.DollyCamera( m_currentDollyDistance );
	}
}
