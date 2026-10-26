// Originally a UNet NetworkBehaviour gated on isLocalPlayer. UNet was removed in
// Unity 2019+, so the project now runs single-player and this always sets up the
// spawned player as the local one.
public class SetUpLocalPlayer : UnityEngine.MonoBehaviour {
    public UnityEngine.GameObject child;
    // Use this for initialization
    void Start () {
        GetComponent<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>().enabled = true;
        child = this.gameObject.transform.GetChild(0).gameObject;
        child.GetComponent<UnityEngine.Camera>().enabled = true;
	}
	
	// Update is called once per frame
	
}
