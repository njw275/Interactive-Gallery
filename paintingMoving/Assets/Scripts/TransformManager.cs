using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script allows moving any game object (when controllers trigger & release)
public class TransformManager : Photon.MonoBehaviour {

	public float speed = 10f;

	PhotonView photonView;

    private Vector3 syncStartPosition; // = transform.position;//Vector3.zero;
    private Vector3 syncEndPosition; // = transform.position; //Vector3.zero;
	private Quaternion syncStartRotation;
	private Quaternion syncEndRotation;


    void Start(){
		photonView = PhotonView.Get (this);
        syncStartPosition = transform.position; // = transform.position;//Vector3.zero;
        syncEndPosition = transform.position; // = transform.position; //Vector3.zero;

		syncStartRotation = transform.rotation; // = transform.position;//Vector3.zero;
		syncEndRotation = transform.rotation; // = transform.position; //Vector3.zero;
	}

	// Update is called once per frame
	void Update () {
		//Update the movement
		if (!photonView.isMine) {
			SyncedMovement ();
		}
	}

	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;

	//Here if we are writing to the stream we send position and velocity
	//otherwise we are reading the position and the velocity from the stream to get the update information
	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		Rigidbody rb = GetComponent<Rigidbody> ();
		if (stream.isWriting)
		{
			
			stream.SendNext(rb.position);
			stream.SendNext(rb.velocity);
			stream.SendNext(rb.rotation);
			stream.SendNext (rb.angularVelocity);
		}
		else
		{
			Vector3 syncPosition = (Vector3)stream.ReceiveNext();
			Vector3 syncVelocity = (Vector3)stream.ReceiveNext();
			Quaternion syncRotation = (Quaternion)stream.ReceiveNext ();


			// New stuff from the internet
			rb.angularVelocity = (Vector3)stream.ReceiveNext ();


			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;

			syncEndPosition = syncPosition + syncVelocity * syncDelay;
			syncStartPosition = rb.position;

			//Trying with Rotation as well
			syncEndRotation = syncRotation; // + angularVelocity * syncDelay;
			syncStartRotation = rb.rotation;

		}
	}

	private void SyncedMovement()
	{
		Rigidbody rb = GetComponent<Rigidbody> ();
		syncTime += Time.deltaTime;
		rb.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
		rb.rotation = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
	}

	public void StartColorChange(Vector3 color){
		photonView.RPC("ChangeColorTo",PhotonTargets.All, color);
	}

	public void StartMoveTo(Vector3 direction){
		photonView.RPC("MoveTo",PhotonTargets.All, direction);
	}

	//Change the color
	[PunRPC] void ChangeColorTo(Vector3 color)
	{
		GetComponent<Renderer>().material.color = new Color(color.x, color.y, color.z, 1f);
		if (photonView.isMine)
			photonView.RPC("ChangeColorTo", PhotonTargets.OthersBuffered, color);
	}

    //Change the color to more transparent
    [PunRPC] public void MakeTransparent() {
        Color myColor = GetComponent<Renderer>().material.color;
        myColor.a = 0.3f;
        GetComponent<Renderer>().material.color = myColor;
        if (photonView.isMine)
            photonView.RPC("MakeTransparent", PhotonTargets.OthersBuffered, photonView.viewID);
    }

    //Change the color to no transparency 
    [PunRPC] public void MakeVisible() {
        Color myColor = GetComponent<Renderer>().material.color;
        myColor.a = 1f;
        GetComponent<Renderer>().material.color = myColor;
        if (photonView.isMine)
            photonView.RPC("MakeVisible", PhotonTargets.OthersBuffered, photonView.viewID);
    }

    //Move the object
    [PunRPC] void MoveTo(Vector3 direction)
	{
		GetComponent<Transform>().position = direction;
		if (photonView.isMine)
			photonView.RPC("MoveTo", PhotonTargets.OthersBuffered, direction);
	}

	//set a new parent
	[PunRPC] public void SetNewParent(Transform tr){
		transform.SetParent (tr);
		transform.position = new Vector3 (tr.position.x, tr.position.y, tr.position.z);
		if (photonView.isMine)
			photonView.RPC("SetNewParent", PhotonTargets.OthersBuffered,tr);
	}

	//detach the parent
	[PunRPC] public void DetachParent(){
		transform.parent = null;
		Debug.Log("Detached all parents");

		if (photonView.isMine)
			photonView.RPC("DetachParent", PhotonTargets.OthersBuffered,photonView.viewID);

	}
}
