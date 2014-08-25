using UnityEngine;
using System.Collections;

public enum InputAction
{
	None,
	Push,
	Plant,
	MakeHouse
}

public class Game : MonoBehaviour {

	public enum GameState
	{
		Initializing,
		CheatSheet,
		Playing,
		Win,
		Lose,
		Disconnected
	}

	public NetworkStuff network;
	public GameObject planetPrefab;
	private Planet planetInstance;
	public GameObject title;

	private float EnergyGatherTime = 10.0f;

	// eww static
	public static float CurrentEnergy = 0;

	private float StartingEnergy = 30;

	public GameState CurrentGameState;

	private bool attemptingConnect = false;

	private float lastEnergyGainTime;

	public InputAction CurrentInputAction;

	public const int PLANTCOST = 10;
	public const int PUSHCOST = 50;
	public const int HOUSECOST = 100;

	public const int GOLDENTREEWINCOUNT = 3;

	public static Color GoldenColor = new Color(255/255.0f, 249/255.0f, 48/255.0f);

	public GameObject cheatSheat;

	public void Start()
	{
		network.NetworkGameStarted += HandleNetworkGameStarted;
		network.Disconnected += HandleDisconnected;

		CurrentGameState = GameState.Initializing;
	}

	public void Update()
	{
		if (CurrentGameState == GameState.Playing)
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				CurrentInputAction = InputAction.None;
			}
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				CurrentInputAction = InputAction.Plant;
			}
			if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				CurrentInputAction = InputAction.Push;
			}
			if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				CurrentInputAction = InputAction.MakeHouse;
			}
			
			if (Input.GetMouseButtonDown(0))
			{
				if (planetInstance.TryAction(CurrentInputAction))
				{
					CurrentInputAction = InputAction.None;
				}
			}

			if (!network.isConnected)
			{
				Debug.Log("No longer connected");
			}
		}
	}

	private void EnergyGather()
	{
		lastEnergyGainTime = Time.time;

		int energyGain = planetInstance.GetEnergyGain();
	  	CurrentEnergy += energyGain;

		Debug.Log("Energy Gained: " + energyGain);
	}

	public void OnGUI()
	{
		GUI.color = Color.black;

		if (CurrentGameState == GameState.CheatSheet)
		{
			if (GUILayout.Button("Back"))
			{
				title.SetActive(true);
				cheatSheat.gameObject.SetActive(false);
				CurrentGameState = GameState.Initializing;
			}
		}
		else if (CurrentGameState == GameState.Initializing)
		{
			ConnectionTesterStatus status = Network.TestConnection();
			GUILayout.Label("Network Status: " + status);
			if (status == ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted)
			{
				GUILayout.Label("Server is port restricted: It make take longer to join games.");
			}
			else if (status != ConnectionTesterStatus.Undetermined)
			{
				GUILayout.Label("You can host games!");
			}

			title.SetActive(true);

			if (!network.isConnected)
			{
				GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();

				GUILayout.BeginVertical();

				if (attemptingConnect || network.isWaitingForConnection)
				{
					GUILayout.Button("Finding an opponent...", GUILayout.Width(200));
					if (GUILayout.Button("Cancel", GUILayout.Width(200)))
					{
						network.Abort();
						Reboot();
					}
				}
				else 
				{				
					GUILayout.Space(10);

					if (GUILayout.Button("Solo Play (no opponent)", GUILayout.Width(200)))
					{
						attemptingConnect = true;
						network.isConnected = true;
						HandleNetworkGameStarted();
					}

					if (GUILayout.Button("How To Play", GUILayout.Width(200)))
					{
						cheatSheat.gameObject.SetActive(true);
						title.SetActive(false);
						CurrentGameState = GameState.CheatSheet;
					}

					GUILayout.Space(20);

					/*
					if (GUILayout.Button("Play", GUILayout.Width(200)))
					{
						attemptingConnect = true;
						network.MakeOrConnectToGame();
					}
					*/

					if (network.NetworkTestStatus == ConnectionTesterStatus.Undetermined)
					{
						GUILayout.Button("Checking Hosting Status...");
					}
					else if (network.CanHost)
					{
						if (GUILayout.Button("Host", GUILayout.Width(200)))
						{
							attemptingConnect = true;
							network.HostGame();
						}
					}
					else
					{
						GUILayout.Button("Cannot Host :(", GUILayout.Width(200));
					}

					if (GUILayout.Button("Join", GUILayout.Width(200)))
					{
						attemptingConnect = true;
						network.JoinGame();
					}
				}

				GUILayout.Label("Available Games: " + network.ValidHostData.Count.ToString());

				/*
				if (GUILayout.Button("About", GUILayout.Width(200)))
				{
					attemptingConnect = true;
					StartCoroutine(network.MakeOrConnectToGame());
				}
				*/

				GUILayout.EndVertical();
				
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.EndArea();
			}
		}
		else if (CurrentGameState == GameState.Playing)
		{
			GUILayout.BeginVertical();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Energy:");
			GUILayout.Label(CurrentEnergy.ToString());
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Time til next gain:");
			float nextTime = Mathf.Clamp(EnergyGatherTime - (Time.time - lastEnergyGainTime), 0, EnergyGatherTime);
			GUILayout.Label(ConvertTimeToString(nextTime));
			GUILayout.EndHorizontal();

			GUILayout.BeginVertical();

			GUI.color = (CurrentInputAction == InputAction.None ? Color.yellow : Color.black);
			if (GUILayout.Button("Esc: None"))
			{
				CurrentInputAction = InputAction.None;
			}
			GUI.color = (CurrentInputAction == InputAction.Plant ? Color.yellow : Color.black);
			if (GUILayout.Button("1-Plant : " + PLANTCOST.ToString()))
			{
				CurrentInputAction = InputAction.Plant;
			}
			GUI.color = (CurrentInputAction == InputAction.Push ? Color.yellow : Color.black);
			if (GUILayout.Button("2-Push : " + PUSHCOST.ToString()))
			{
				CurrentInputAction = InputAction.Push;
			}
			GUI.color = (CurrentInputAction == InputAction.MakeHouse ? Color.yellow : Color.black);
			if (GUILayout.Button("3-House : " + HOUSECOST))
			{
				CurrentInputAction = InputAction.MakeHouse;
			}

			GUILayout.EndVertical();

			GUILayout.EndVertical();

			float width = 100;
			if (GUI.Button(new Rect(10, Screen.height - 30, width, 20), "Quit"))
			{
				network.Abort();
				Reboot();
			}
		}
		else if (CurrentGameState == GameState.Lose)
		{
			float width = 350;

			var rect = new Rect(Screen.width/2.0f - width/2.0f, Screen.height - 70, width, 30);
			GUI.Label(rect, "Game Over! The other player created 3 golden trees first!");

			float buttonWidth = 100;
			var buttonRect = new Rect(Screen.width/2.0f - buttonWidth/2.0f, Screen.height - 40, buttonWidth, 20);
			if (GUI.Button(buttonRect, "Main Menu"))
			{
				Reboot();
			}
		}
		else if (CurrentGameState == GameState.Win)
		{
			float width = 350;
			
			var rect = new Rect(Screen.width/2.0f - width/2.0f, Screen.height - 70, width, 30);
			GUI.Label(rect, "Congratulations!  You were the first to create 3 golden trees!");
			
			float buttonWidth = 100;
			var buttonRect = new Rect(Screen.width/2.0f - buttonWidth/2.0f, Screen.height - 40, buttonWidth, 20);
			if (GUI.Button(buttonRect, "Main Menu"))
			{
				Reboot();
			}
		}
		else if (CurrentGameState == GameState.Disconnected)
		{
			float width = 200;
			
			var rect = new Rect(Screen.width/2.0f - width/2.0f, Screen.height - 70, width, 30);
			GUI.Label(rect, "Your opponent disconnected.");
			
			float buttonWidth = 100;
			var buttonRect = new Rect(Screen.width/2.0f - buttonWidth/2.0f, Screen.height - 40, buttonWidth, 20);
			if (GUI.Button(buttonRect, "Main Menu"))
			{
				Reboot();
			}
		}
	}

	private void Reboot()
	{
		if (planetInstance != null && planetInstance.gameObject != null)
			GameObject.Destroy(planetInstance.gameObject);

		network.ScanForGames = true;
		attemptingConnect = false;
		CurrentGameState = GameState.Initializing;
	}

	public static string ConvertTimeToString(float time)
	{
		System.TimeSpan ts = new System.TimeSpan(0, 0, 0, 0, Mathf.FloorToInt(time*1000.0f));
		System.DateTime dt = new System.DateTime(ts.Ticks);
		return dt.ToString("ss.f");
	}

	public void HandleNetworkGameStarted()
	{
		planetInstance = ((GameObject)Instantiate(planetPrefab)).GetComponent<Planet>();

		planetInstance.GameLose += HandleGameLose;
		planetInstance.GameWin += HandleGameWin;

		StartGame();
	}

	private void HandleGameWin(string reason)
	{
		CurrentGameState = GameState.Win;
		//GameWinReason = reason;
		CancelInvoke("EnergyGather");
	}

	private string GameLoseReason;
	private void HandleGameLose(string reason)
	{
		CurrentGameState = GameState.Lose;
		GameLoseReason = reason;
		CancelInvoke("EnergyGather");
	}

	public void HandleDisconnected()
	{
		if (CurrentGameState == GameState.Playing)
		{
			MasterServer.UnregisterHost();
			CurrentGameState = GameState.Disconnected;
			CancelInvoke("EnergyGather");
		}
	}

	private void StartGame()
	{
		title.SetActive(false);
		CurrentGameState = GameState.Playing;
		CurrentEnergy = StartingEnergy;

		lastEnergyGainTime = Time.time;
		InvokeRepeating("EnergyGather", EnergyGatherTime, EnergyGatherTime);

		planetInstance.Initialize();
	}
}
