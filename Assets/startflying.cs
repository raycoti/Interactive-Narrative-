using UnityEngine;
using System.Collections;

public class startflying : MonoBehaviour {
    GameObject theobject;
    Animator anim;
	// Use this for initialization
	void Start () {
        theobject = GameObject.Find("flybridge");
        anim = theobject.GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update () {
	
	}
    void OnTriggerEnter(Collider collision)
    {
        anim.Play("ship");
        print("hello");//start transformation sequence yooooo!!! 
    }
}
