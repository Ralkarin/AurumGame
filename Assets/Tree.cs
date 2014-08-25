using UnityEngine;
using System.Collections;

public class Tree : MonoBehaviour {

	public bool IsGolden = false;
	public MeshRenderer tree1;
	public MeshRenderer tree2;
	public MeshRenderer tree3;

	public void MakeGolden()
	{
		IsGolden = true;
		Color newColor = Game.GoldenColor;
		tree1.material.color = newColor;
		tree2.material.color = newColor;
		tree3.material.color = newColor;
	}
}
