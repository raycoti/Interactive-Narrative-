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
    // How far past the box's trigger pressing back glides the player.
    public float backAway = 2f;
    private float nextone;
    private bool entered = false;
    // A held player who left the trigger; the poem keeps going until they walk on.
    private UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController leaving;
    // The player last stopped here. They aren't stopped again until they've moved this far
    // clear of the trigger, so brushing its edge on the way out can't trap them.
    private UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController reader;
    private float clearance;
	// Use this for initialization
	void Start () {
        thetext = GetComponent<TextMesh>();
	}
	
	// Update is called once per frame
	void Update () {
        if (leaving != null && !leaving.Held)
        {
            leaving = null;
            StopReading();
        }
        if (reader != null && !reader.Held)
        {
            var p = reader.transform.position;
            if (Vector3.Distance(GetComponent<BoxCollider>().ClosestPoint(p), p) > clearance)
                reader = null;
        }
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
        leaving = null;
        entered = true;
        nextone = Time.time + timetoread;
        // Stop the player so the poem can be read; a walk key releases them (see HoldUntilWalk).
        var player = other.GetComponentInParent<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>();
        if (player != null && player != reader)
        {
            var lookAt = thetext.GetComponent<Renderer>().bounds.center;
            // The text reads from the side facing away from its forward, where the box's front is.
            var front = -transform.forward;
            front.y = 0f;
            front.Normalize();
            var box = GetComponent<BoxCollider>();
            var boxCentre = transform.TransformPoint(box.center);
            // Stand with the player's capsule just overlapping this trigger, so leaving it
            // doesn't stop the poem. The solid box inside is smaller, so physics can't push
            // the player out of the trigger.
            var capsule = other as CapsuleCollider;
            var radius = capsule != null ? capsule.radius * Mathf.Max(other.transform.lossyScale.x, other.transform.lossyScale.z) : 0.5f;
            var halfDepth = box.size.z * 0.5f * transform.lossyScale.z;
            var standAt = boxCentre + front * (halfDepth + radius - 0.1f);
            var side = front;
            // From behind the box, gliding would go through it, so just turn to face the text.
            if (Vector3.Dot(other.transform.position - boxCentre, front) < 0f)
            {
                standAt = other.transform.position;
                side = -front;
            }
            // Backing away glides straight out from the box until the player is clear of it.
            var outTo = halfDepth + radius + backAway;
            var leaveTo = standAt + side * Mathf.Max(0f, outTo - Vector3.Dot(standAt - boxCentre, side));
            reader = player;
            clearance = radius + 0.25f;
            player.HoldUntilWalk(standAt, lookAt, leaveTo);
        }
    }
   
    void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>();
        if (player != null && player.Held)
        {
            leaving = player;
            return;
        }
        StopReading();
    }

    void StopReading()
    {
        thetext.text = welcome;
        entered = false;
        print("hello");
    }
}
