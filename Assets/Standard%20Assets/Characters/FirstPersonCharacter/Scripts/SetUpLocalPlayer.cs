using UnityEngine.Networking;

public class SetUpLocalPlayer : NetworkBehaviour {
    public UnityEngine.GameObject child;
    // Use this for initialization
    void Start () {
        
        if (isLocalPlayer)
        {
            GetComponent<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>().enabled = true;
            child = this.gameObject.transform.GetChild(0).gameObject;
            child.GetComponent<UnityEngine.Camera>().enabled = true;
            // GetComponent<UnityEngine.Camera>().enabled = true;
        }
	}
	
	// Update is called once per frame
	
}
