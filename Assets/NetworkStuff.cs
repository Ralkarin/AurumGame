using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class NetworkStuff : MonoBehaviour {

	[NonSerialized]
	public int NumberOfPlayers = 1;
	[NonSerialized]
	public int ConnectionPort = 25565;

	//public string GameTypeName = "Ralkarin-ConnectedWorlds";
	public string GameTypeName = "Ralkarin-ConnectedWorlds-v1";

	public string GameNameRoot = "ConnectedWorlds";

	private Ping masterServerPing;
	public List<HostData> ValidHostData = new List<HostData>();

	public bool isConnected = false;
	private bool abortAttemptHost = false;
	public bool isWaitingForConnection = false;

	public System.Action NetworkGameStarted;
	public System.Action Disconnected;

	private string myGameName;

	public ConnectionTesterStatus NetworkTestStatus;

	public bool ScanForGames = true;

	public void Start()
	{
		myGameName = GameNameRoot + System.Guid.NewGuid();

		StartCoroutine(DetermineNetworkStatus());

		InvokeRepeating("FindGamesToJoin", 0, 5.0f);
	}

	private IEnumerator DetermineNetworkStatus()
	{
		do
		{
			NetworkTestStatus = Network.TestConnection();
			yield return null;
		} while (NetworkTestStatus == ConnectionTesterStatus.Undetermined);
	}

	public void MakeOrConnectToGame()
	{
		StopAllCoroutines();
		StartCoroutine(MakeOrConnectToGame_Routine());
	}

	private bool scanning = false;
	public void FindGamesToJoin()
	{
		if (ScanForGames && !scanning)
		{
			scanning = true;

			Debug.Log("Clearing Host List.");
			MasterServer.ClearHostList();

			Debug.Log("Requesting Host List.");
			MasterServer.RequestHostList(GameTypeName);
		}

		scanning = false;
	}

	public void Abort()
	{
		if (isConnected || isWaitingForConnection)
		{
			isConnected = false;
			isWaitingForConnection = false;

			Network.Disconnect();
		}

		MasterServer.UnregisterHost();

		StopAllCoroutines();
	}

	public bool CanHost
	{
		get
		{
			return (NetworkTestStatus != ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted);
		}
	}

	public void HostGame()
	{
		ScanForGames = false;

		if (NetworkTestStatus != ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted)
		{
			NetworkConnectionError error = Network.InitializeServer(NumberOfPlayers, ConnectionPort, !Network.HavePublicAddress());

			Debug.Log(error);

			if (error == NetworkConnectionError.NoError)
			{
				MasterServer.RegisterHost(GameTypeName, myGameName, "Open");
				Debug.Log("Waiting for client connection...");
				isWaitingForConnection = true;
			}
			else
			{
				abortAttemptHost = true;
			}
		}
	}

	public void JoinGame()
	{
		isWaitingForConnection = true;
		StartCoroutine(ScanJoinGameCoroutine());
	}
	
	private IEnumerator ScanJoinGameCoroutine()
	{
		while (!isConnected)
		{
			List<HostData> copyHosts = new List<HostData>(ValidHostData);

			// Step 1: Attempt to connect to existing clients
			foreach(HostData host in copyHosts)
			{
				// give it a shot!
				NetworkConnectionError error = Network.Connect(host);
				Debug.Log("NetworkConnect Result: " + error);

				if (error == NetworkConnectionError.NoError)
				{
					break;
				}
			}

			if (!isConnected)
				yield return new WaitForSeconds(1.0f);
		}

		yield break;
	}

	private IEnumerator MakeOrConnectToGame_Routine()
	{
		while (!isConnected)
		{
			abortAttemptHost = false;

			// First try to find a public server 
			HostData[] hostData = new HostData[0];
			MasterServer.UnregisterHost();
			Debug.Log("Clearing Host List.");
			MasterServer.ClearHostList();
			Debug.Log("Requesting Host List.");
			MasterServer.RequestHostList(GameTypeName);

			float startTime = Time.time;
			while (Time.time - startTime < 10) // attempt to host for some time before trying to join again
			{
				if (isConnected || abortAttemptHost)
					break;
				
				yield return new WaitForSeconds(0.5f);
			}
		}
	}

	void OnMasterServerEvent(MasterServerEvent msEvent)
	{
		Debug.Log("MSEvent: " + msEvent);

		if (msEvent == MasterServerEvent.HostListReceived)
		{
			Debug.Log("Master Server Host List Received");

			HostData[] hostData = MasterServer.PollHostList();

			ValidHostData = new List<HostData>();

			for (int i = 0; i < hostData.Length; i++)
			{
				HostData host = hostData[i];

				Debug.Log("Found Host: " + host.gameName + "IP: " + host.ip[0] + ":" + host.port);
				
				if (host.gameName == myGameName)
				{
					Debug.Log("found my own server, skipping");
					continue;
				}
				
				if (host.comment == "Closed")
				{
					Debug.Log("Too many players, checking next host");
					continue;
				}

				ValidHostData.Add(host);
			}

			/*
			bool connectedToClient = false;
			// Step 1: Attempt to connect to existing clients
			for (int i = 0; i < hostData.Length; i++)
			{
				HostData host = hostData[i];
				

				
				// Automatically connect to the first host
				// TODO: optionally sort the list by shorted ping time
				
				NetworkConnectionError error = Network.Connect(host);
				if (error == NetworkConnectionError.TooManyConnectedPlayers)
				{
					Debug.Log("too many players");
					isConnected = false;
					continue;
				}
				else if (error == NetworkConnectionError.NoError)
				{
					StartNetworkedGame();
					connectedToClient = true;
					break;
				}
			}

			// Try to start a server if you can
			if (!connectedToClient && NetworkTestStatus != ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted)
			{
				Debug.Log("No hosts found - starting server");
				
				// Can't find any public servers, let's be a server and wait for someone to connect!
				myGameName = GameNameRoot + System.Guid.NewGuid();
				NetworkConnectionError error = Network.InitializeServer(NumberOfPlayers, ConnectionPort, !Network.HavePublicAddress());
				if (error == NetworkConnectionError.NoError)
				{
					Debug.Log(error);
					MasterServer.RegisterHost(GameTypeName, myGameName);
				}
				else
				{
					abortAttemptHost = true;
				}

				Debug.Log("Waiting for client connection...");
			}
			*/
		}
	}

	private void StartNetworkedGame()
	{
		isWaitingForConnection = false;
		isConnected = true;
		NetworkGameStarted();
	}

	void OnPlayerConnected(NetworkPlayer networkPlayer)
	{
		Debug.Log("Server: Player Joined");

		MasterServer.RegisterHost(GameTypeName, myGameName, "Closed");
		StartNetworkedGame();
	}

	void OnPlayerDisconnected()
	{
		Debug.Log("Server: Player Disconnected");

		// Close the server connection
		Network.Disconnect();

		Abort();
		Disconnected();
	}

	void OnConnectedToServer()
	{
		Debug.Log("Client: Connected to Server");

		// Game Go!
		StartNetworkedGame();
	}

	void OnDisconnectedFromServer()
	{
		Debug.Log("Client: Disconnected from Server");

		if (Network.isClient)
		{
			Abort();
			Disconnected();
		}
	}

	void OnServerInitialized()
	{
		Debug.Log("Server: Server Initializied");
	}
}
