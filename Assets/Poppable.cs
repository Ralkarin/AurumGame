using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Poppable : MonoBehaviour {

	public void Pop()
	{
		this.rigidbody.AddForce(transform.position * 50.0f);
		this.rigidbody.AddTorque(Random.insideUnitSphere * 200.0f);

		GetComponent<SphereCollider>().enabled = false;

		Destroy(this.gameObject, 1.0f);
	}
}
