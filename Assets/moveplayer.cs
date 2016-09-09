using UnityEngine;
using System.Collections;

public class moveplayer : MonoBehaviour {
    public GameObject gotothis;
    private float x;
    private float y;
    private float z;
	// Use this for initialization
	void Start () {
        x = gotothis.transform.position.x;
        y = gotothis.transform.position.y;
        z = gotothis.transform.position.z;
    }
	
	// Update is called once per frame
	void Update () {
	
	}
    void OnTriggerEnter(Collider collision)
    {
        collision.transform.position = new Vector3(x, y + 50, z);
    }
}
