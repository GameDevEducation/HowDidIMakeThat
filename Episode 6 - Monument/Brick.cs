using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Brick : MonoBehaviour {
	public TMPro.TextMeshPro BrickTextMesh;
	public MeshRenderer BrickRenderer;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void SetMessage(Message brickMessage, Monument monument)
	{
		// update the text
		string newText = brickMessage.DisplayMessage;
		if (string.IsNullOrEmpty(brickMessage.DisplayAuthor))
			newText += "\n - Anonymous -";
		else
			newText += "\n - " + brickMessage.DisplayAuthor + " -";
		BrickTextMesh.text = newText;

		// update the colour
		BrickRenderer.material = monument.BrickMaterials[(int)brickMessage.colour];
	}
}
