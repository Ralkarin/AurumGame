using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Planet : MonoBehaviour {

	MeshFilter planetMesh;
	public GameObject FlowerPrefab;
	public GameObject TreePrefab;
	public GameObject StumpPrefab;
	public GameObject TotemPrefab;
	public GameObject HousePrefab;
	public GameObject VolcanoPrefab;
	public GameObject WaterNodePrefab;

	public LayerMask ClickLayerMask;
	public LayerMask ObjectsLayerMask;

	public int GoldenTreeCount = 0;

	public System.Action<string> GameLose;
	public System.Action<string> GameWin;

	// Use this for initialization
	void Awake () {
		IcoSphere.Create(gameObject);
		planetMesh = gameObject.GetComponent<MeshFilter>();
	}

	public enum TerrainState
	{
		Raised,
		Ground,
		Lowered
	}

	public void Update()
	{
		if (Input.GetKey(KeyCode.A))
		{
			gameObject.transform.Rotate(new Vector3(0, 1, 0) * Time.deltaTime * 60);
		}

		if (Input.GetKey(KeyCode.D))
		{
			gameObject.transform.Rotate(new Vector3(0, 1, 0) * Time.deltaTime * -60);
		}
	}

	public void Initialize()
	{
		int range = planetMesh.mesh.vertexCount;
		List<int> randomValues = new List<int>();

		while (randomValues.Count < 3)
		{
			int nextValue = Random.Range(0, range);
			if (!randomValues.Contains(nextValue))
				randomValues.Add(nextValue);
		}

		for (int i = 0; i < randomValues.Count; i++)
		{
			MakeHouse(planetMesh.mesh.vertices[randomValues[i]]);
		}
	}

	public bool TryAction(InputAction currentInputAction)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, 1000, ClickLayerMask))
		{
			int index = -1;
			Vector3 vertex = NearestVertexTo(hitInfo.point, out index);
			Debug.Log("Nearest Vertex: " + vertex);
			
			TerrainState terrainState = TerrainState.Ground;
			if (vertex.magnitude <= 0.9f)
				terrainState = TerrainState.Lowered;
			else if (vertex.magnitude >= 1.1f)
				terrainState = TerrainState.Raised;
			
			Collider[] otherObjects = Physics.OverlapSphere(vertex, 0.05f, ObjectsLayerMask);
			Debug.Log("other objects: " + otherObjects.Length);
			
			switch (currentInputAction)
			{
			case InputAction.None:
				if (hitInfo.collider.GetComponent<Tree>())
				{
					StartTreeDrag();
				}
				else if (terrainState == TerrainState.Lowered)
				{
					/*
					Volcano(index);
					networkView.RPC("NotifyVolcanoLaunch", RPCMode.Others, index);
					*/
				}
				break;
			case InputAction.MakeHouse:
				if (terrainState == TerrainState.Ground
				    && otherObjects.Length == 0
				    && Game.CurrentEnergy >= Game.HOUSECOST)
				{
					Game.CurrentEnergy -= Game.HOUSECOST;
					
					MakeHouse(vertex);
					// no network call required, client side information only.  ;)

					return true;
				}
				break;
			case InputAction.Push:
				if (otherObjects.Length > 0 
				    && otherObjects[0].gameObject.name.Contains("Stump")
				    && Game.CurrentEnergy >= Game.PUSHCOST)
				{
					Game.CurrentEnergy -= Game.PUSHCOST;

					GameObject.Destroy(otherObjects[0].gameObject);
					networkView.RPC("NotifyPushStump", RPCMode.Others, index);

					return true;
				}
				else if (terrainState != TerrainState.Lowered
				    && Game.CurrentEnergy >= Game.PUSHCOST
					&& otherObjects.Length == 0)
				{
					Game.CurrentEnergy -= Game.PUSHCOST;
					
					float newValue = .8f;
					if (vertex.magnitude > 1.1f)
						newValue = 1.0f;
					
					ModifyMesh(index, newValue);

					networkView.RPC("NotifyModifyMesh", RPCMode.Others, index, newValue);

					if (newValue < .9f)
					{
						CheckPlaceWaterAtIndex(index);
					}

					return true;
				}
				break;
			/*
			case InputAction.RaiseTerrain:
				if (terrainState != TerrainState.Raised
				    && Game.CurrentEnergy >= 5)
				{
					Game.CurrentEnergy -= 5;
					
					ModifyMesh(index, 1.2f);
					networkView.RPC("NotifyModifyMesh", RPCMode.Others, index, 1.2f);

					return true;
				}
				break;
			*/
			case InputAction.Plant:
				if (terrainState == TerrainState.Ground
				    && otherObjects.Length == 0
				    && Game.CurrentEnergy >= Game.PLANTCOST)
				{
					Game.CurrentEnergy -= Game.PLANTCOST;
					
					CheckPlaceFlowerAtIndex(index);

					return true;
				}
				break;
			}
		}

		return false;
	}
	
	private void CheckPlaceWaterAtIndex(int index)
	{
		Vector3 worldPos = transform.TransformPoint(planetMesh.mesh.vertices[index]);
		Collider[] otherObjects = Physics.OverlapSphere(worldPos, 0.2f, ObjectsLayerMask);

		Debug.DrawRay(worldPos, worldPos.normalized*0.2f, Color.red, 1.0f);
		foreach (Collider other in otherObjects)
		{
			if (other.gameObject.name.Contains("Flower"))
			{
				GameObject.Destroy(other.gameObject);
				int newindex;
				NearestVertexTo(other.gameObject.transform.position, out newindex);
				PlantTree(newindex);
				networkView.RPC("NotifyTreePlanted", RPCMode.Others, newindex);
			}
		}
	}

	private void CheckPlaceFlowerAtIndex(int index)
	{
		Vector3 worldPos = transform.TransformPoint(planetMesh.mesh.vertices[index]);
		Collider[] otherObjects = Physics.OverlapSphere(worldPos, 0.2f, ObjectsLayerMask);

		bool indexHasWaterNeighor = false;
		int treeCount = 0;
		foreach (Collider other in otherObjects)
		{
			if (other.gameObject.tag == "WaterNode")
			{
				indexHasWaterNeighor = true;
			}

			if (other.gameObject.GetComponent<Tree>() != null)
			{
				treeCount++;
			}
		}

		if (treeCount == 6)
		{
			GameObject treeObject = PlantTree(index);
			treeObject.GetComponent<Tree>().MakeGolden();
			networkView.RPC("NotifyCreatedGoldenTree", RPCMode.Others, index);

			AddGoldenTree();
		}
		else if (indexHasWaterNeighor)
		{
			PlantTree(index);
			networkView.RPC("NotifyTreePlanted", RPCMode.Others, index);
		}
		else
		{
			PlantFlower(index);
		}
	}

	private void AddGoldenTree()
	{
		GoldenTreeCount++;

		if (GoldenTreeCount >= Game.GOLDENTREEWINCOUNT)
		{
			StartCoroutine(WinGame("Opponent won by golden tree victory!"));
		}
	}

	private IEnumerator WinGame(string reason)
	{
		GameWin("Hello");

		networkView.RPC("NotifyOtherPlayerWin", RPCMode.Others, reason);

		foreach(MeshRenderer render in gameObject.GetComponentsInChildren<MeshRenderer>())
		{
			render.material.color = Game.GoldenColor;

			yield return new WaitForSeconds(0.05f);
		}

		yield break;
	}

	[RPC]
	public void NotifyOtherPlayerLose(string reason)
	{
		GameWin(reason);
		Network.Disconnect();
	}

	[RPC]
	public void NotifyOtherPlayerWin(string reason)
	{
		GameLose(reason);
		Network.Disconnect();
	}

	private void Volcano(int index)
	{
		Debug.Log("client side volcano stuff here");
	}

	[RPC]
	public void NotifyVolcanoLaunch(int index)
	{
		Vector3 worldPos = transform.TransformPoint(planetMesh.mesh.vertices[index]);
		
		GameObject instance = (GameObject)Instantiate(VolcanoPrefab);
		instance.transform.parent = this.transform;
		instance.transform.position = worldPos;
		instance.transform.up = worldPos;

		Volcano volcano = instance.GetComponent<Volcano>();
		volcano.Explode();
	}

	private void StartTreeDrag()
	{
		Debug.Log("hi");
	}

	public int GetEnergyGain()
	{
		return gameObject.GetComponentsInChildren<House>().Length * 10;
	}

	public GameObject PlantFlower(int index)
	{
		Vector3 vertex = transform.TransformPoint(planetMesh.mesh.vertices[index]);
		
		GameObject treeInstance = (GameObject)Instantiate(FlowerPrefab);
		treeInstance.transform.parent = this.transform;
		
		treeInstance.transform.position = vertex;
		treeInstance.transform.up = vertex;
		
		return treeInstance;
	}

	public GameObject PlantTree(int index)
	{
		Vector3 vertex = transform.TransformPoint(planetMesh.mesh.vertices[index]);

		GameObject treeInstance = (GameObject)Instantiate(TreePrefab);
		treeInstance.transform.parent = this.transform;

		treeInstance.transform.position = vertex;
		treeInstance.transform.up = vertex;

		return treeInstance;
	}

	[RPC]
	public void NotifyTreePlanted(int index)
	{
		Vector3 worldPos = transform.TransformPoint(planetMesh.mesh.vertices[index]);

		CheckPopObjectAtIndex(index);

		GameObject instance = (GameObject)Instantiate(StumpPrefab);
		instance.transform.parent = this.transform;
		instance.transform.position = worldPos;
		instance.transform.up = worldPos;
	}

	[RPC]
	public void NotifyCreatedGoldenTree(int index)
	{
		Vector3 worldPos = transform.TransformPoint(planetMesh.mesh.vertices[index]);
		
		Collider[] otherObjects = Physics.OverlapSphere(worldPos, 0.05f, ObjectsLayerMask);
		if (otherObjects.Length > 0)
		{
			GameObject.Destroy(otherObjects[0].gameObject);
		}
		
		GameObject instance = (GameObject)Instantiate(TotemPrefab);
		instance.transform.parent = this.transform;
		instance.transform.position = worldPos;
		instance.transform.up = worldPos;
	}

	[RPC]
	public void NotifyPushStump(int index)
	{
		CheckPopObjectAtIndex(index);
	}

	private void CheckPopObjectAtIndex(int index)
	{
		Vector3 worldPos = transform.TransformPoint(planetMesh.mesh.vertices[index]);
		
		Collider[] otherObjects = Physics.OverlapSphere(worldPos, 0.05f, ObjectsLayerMask);
		if (otherObjects.Length > 0)
		{
			Poppable poppable = otherObjects[0].GetComponent<Poppable>();
			if (poppable != null)
				poppable.Pop();
		}
	}

	public void ModifyMesh(int index, float modifier)
	{
		Vector3[] vertices = planetMesh.mesh.vertices;
		Vector3[] normals = planetMesh.mesh.normals;
		Color32[] colors = planetMesh.mesh.colors32;
		
		// Up!
		vertices[index] = normals[index] * modifier;

		Debug.Log("Old vert color: " + colors[index]);
		if (vertices[index].magnitude >= .99f && vertices[index].magnitude <= 1.01f)
			//colors[index] = new Color32(145, 188, 221, 255);
			colors[index] = new Color32(255, 255, 255, 255);
		else
			colors[index] = new Color32(100, 100, 100, 255);

		planetMesh.mesh.colors32 = colors;
		planetMesh.mesh.vertices = vertices;

		// pop object if someone raises land under your thing!
		if (modifier > 1.1f)
		{
			CheckPopObjectAtIndex(index);
		}

		Vector3 worldPos = transform.TransformPoint(vertices[index]);
		if (modifier < .99f)
		{
			GameObject waterNode = (GameObject)Instantiate(WaterNodePrefab);
			waterNode.transform.position = worldPos;
			waterNode.transform.parent = this.transform;
		}
		else
		{
			Collider[] otherObjects = Physics.OverlapSphere(worldPos, 0.05f, ObjectsLayerMask);
			if (otherObjects.Length > 0)
			{
				if (otherObjects[0].tag == "WaterNode")
				{
					GameObject.Destroy(otherObjects[0].gameObject);
				}
			}
		}
	}

	[RPC]
	public void NotifyModifyMesh(int index, float modifier)
	{
		Debug.Log("other client changed mesh!");

		float newModifier = 1.0f;
		if (modifier == 0.8f)
			newModifier = 1.2f;
		else if (modifier == 1.2f)
			newModifier = 0.8f;

		ModifyMesh(index, newModifier);
	}

	public void MakeHouse(Vector3 vertex)
	{
		GameObject instance = (GameObject)Instantiate(HousePrefab);
		instance.transform.parent = this.transform;
		
		instance.transform.position = vertex;
		instance.transform.up = vertex;
	}
	
	public Vector3 NearestVertexTo(Vector3 point, out int index)
	{
		index = -1;

		// convert point to local space
		point = transform.InverseTransformPoint(point);

		Mesh mesh = GetComponent<MeshFilter>().mesh;
		float minDistanceSqr = Mathf.Infinity;
		Vector3 nearestVertex = Vector3.zero;
		
		// scan all vertices to find nearest
		for (int i = 0; i < mesh.vertices.Length; i++)
		{
			Vector3 diff = point-mesh.vertices[i].normalized;
			float distSqr = diff.sqrMagnitude;
			
			if (distSqr < minDistanceSqr)
			{
				minDistanceSqr = distSqr;
				nearestVertex = mesh.vertices[i];
				index = i;
			}
		}
		
		// convert nearest vertex back to world space
		return transform.TransformPoint(nearestVertex);
	}
}
