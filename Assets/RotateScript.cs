using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateScript : MonoBehaviour {

	public Vector3 target;
	public float distance;
	public float theta;

	public float rotation;
	public float rotSpeed;
	
	// Update is called once per frame
	void Update () {
		rotation += Time.deltaTime * rotSpeed;
		transform.position = new Vector3(distance * Mathf.Sin(rotation),  distance * Mathf.Atan(theta), distance * Mathf.Cos(rotation));
		transform.LookAt(target);
	}
}
