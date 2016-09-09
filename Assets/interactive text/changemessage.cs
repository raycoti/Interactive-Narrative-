using UnityEngine;
using System.Collections;

public class changemessage : MonoBehaviour {
    public TextMesh thetext;
    public TextMesh text2;
    public TextMesh text3;
    public string[] words;
    public string[] words2;
    
    private int counter = 0;
    public float timetoread=3;
    public string welcome;
    private float nextone;
    private bool entered = false;
	// Use this for initialization
	void Start () {
        thetext = GetComponent<TextMesh>();
	}
	
	// Update is called once per frame
	void Update () {
	    if (entered == true)
        {
            print("hi");
            
            thetext.text = words[counter];
            text2.text = words2[counter];
            text3.text = words2[counter];
            if (counter + 1 < words.Length && Time.time > nextone)
            {
                counter = counter + 1;
                nextone = Time.time + timetoread;
            }
            else if (counter + 1 == words.Length && Time.time > nextone)
            {
                counter = 0;
                nextone = Time.time + timetoread;
            }
        }
	}
    void OnTriggerEnter(Collider other)
    {
        entered = true;
        nextone = Time.time + timetoread;
        print("yo");
    }
   
    void OnTriggerExit(Collider other)
    {
        thetext.text = welcome;
        entered = false;
        print("hello");
    }
}
