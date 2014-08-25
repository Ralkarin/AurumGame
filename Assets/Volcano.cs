using UnityEngine;
using System.Collections;

public class Volcano : MonoBehaviour {

	public GameObject LavaRockPrefab;

	public void Explode()
	{
		int rocks = Random.Range(3, 5);
		for (int i = 0; i < rocks; i++)
		{
			GameObject rockInstance = (GameObject)Instantiate(LavaRockPrefab);
			LavaRock lavaRock = rockInstance.GetComponent<LavaRock>();
			lavaRock.transform.position = this.transform.position;
			lavaRock.Launch(Random.insideUnitSphere + this.transform.up*2.0f, Vector3.zero);//-this.transform.up);
		}

		Destroy(gameObject, 5.0f);
	}
}
