using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class LavaRock : MonoBehaviour {

	private Vector3 gravityVector;
	private float launchTime;

	private float LifeTime = 1.0f;

	public void Launch(Vector3 initialDirection, Vector3 gravityVector)
	{
		launchTime = Time.time;
		rigidbody.AddTorque(Random.insideUnitSphere * 100.0f);
		rigidbody.AddForce(initialDirection.normalized * 50.0f);
		this.gravityVector = gravityVector;
	}
	
	void FixedUpdate()
	{
		rigidbody.AddForce(5f * (gravityVector-this.transform.position));
	}

	public void Update()
	{
		if (Time.time - launchTime > LifeTime)
		{
			GameObject.Destroy(gameObject);
		}
	}
}
