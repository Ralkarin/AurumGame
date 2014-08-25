using UnityEngine;
using System.Collections;
using System;

public class NetworkStuff : MonoBehaviour {

	[NonSerialized]
	public int NumberOfPlayers = 1;
	[NonSerialized]
	public int ConnectionPort = 25565;

	public string GameTypeName = "Ralkarin-ConnectedWorlds-DEBUG";
	//public string GameTypeName = "Ralkarin-ConnectedWorlds";
	public string GameNameRoot = "ConnectedWorlds";

	private Ping masterServerPing;
	private HostData[] hostData;

	public bool isConnected = false;
	private bool abortAttemptHost = false;

	public System.Action NetworkGameStarted;
	public System.Action Disconnected;

	private string myGameName;

	private ConnectionTesterStatus networkTestStatus;

	public void MakeOrConnectToGame()
	{
		StopAllCoroutines();
		StartCoroutine(MakeOrConnectToGame_Routine());
	}

	public void Abort()
	{
		if (isConnected)
		{
			isConnected = false;
			Network.Disconnect();
		}

		MasterServer.UnregisterHost();

		StopAllCoroutines();
	}

	private IEnumerator MakeOrConnectToGame_Routine()
	{
		networkTestStatus = Network.TestConnection();

		int attempts = 0;
		while (networkTestStatus == ConnectionTesterStatus.Undetermined && attempts < 30)
		{
			attempts++;
			yield return new WaitForSeconds(1.0f);
			networkTestStatus = Network.TestConnection();
		}

		masterServerPing = new Ping(MasterServer.ipAddress);

		Debug.Log("Master Server Ping Time: " + masterServerPing.time);

		while (!isConnected)
		{
			abortAttemptHost = false;

			// First try to find a public server 
			hostData = new HostData[0];
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

			hostData = MasterServer.PollHostList();

			bool connectedToClient = false;
			// Step 1: Attempt to connect to existing clients
			for (int i = 0; i < hostData.Length; i++)
			{
				HostData host = hostData[i];
				
				Debug.Log("Found Host: " + host.gameName + "IP: " + host.ip[0] + ":" + host.port);
				
				if (host.gameName == myGameName)
				{
					Debug.Log("found my own server, skipping");
					continue;
				}
				
				if (host.connectedPlayers > 1)
				{
					Debug.Log("Too many players, checking next host");
					continue;
				}
				
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
			if (!connectedToClient && networkTestStatus != ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted)
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
		}
	}

	private void StartNetworkedGame()
	{
		isConnected = true;
		NetworkGameStarted();
	}

	void OnPlayerConnected(NetworkPlayer networkPlayer)
	{
		Debug.Log("Server: Player Joined");
		StartNetworkedGame();
	}

	void OnPlayerDisconnected()
	{
		Debug.Log("Server: Player Disconnected");
		Abort();
		Disconnected();
	}

	void OnConnectedToServer()
	{
		Debug.Log("Client: Connected to Server");
	}

	void OnDisconnectedFromServer()
	{
		Debug.Log("Client: Disconnected from Server");
		Abort();
		Disconnected();
	}

	void OnServerInitialized()
	{
		Debug.Log("Server: Server Initializied");
	}
}
