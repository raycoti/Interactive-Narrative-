using UnityEngine;
using System.Collections;

public class creator : MonoBehaviour {
    public Transform cube;
    public Transform tree;
    public Transform sphere;
    //  GameObject cylin;
    // Use this for initialization
    void Start () {
         
        for (int faze = 1; faze < 400; faze= faze + 100)
        {
            

            for (int i = 1; i <25; i++)
            {
                for (int j = 1; j < 65; j++)
                {
                    //Transform cube;


                    var obj = Instantiate(tree) as Transform;


                    var num1 = Random.Range(7, 14);
                    var num2 = Random.Range(8, 16);
                    //basic objects like the cubes will be more frequent
                    // one of a kind objects will have their own positions given
                    obj.transform.position = new Vector3(((i* num1) + 180), 0, ((j * num2)+faze));
                    obj.gameObject.GetComponent<Renderer>().material.color = new Color(0.427451f, 0.321569f, 0.176471f);
                    // brownish .445,.27,.075
                    // 0.427451 0.321569 0.176471
                }
            }
        }
        for (int faze2 = 1; faze2 < 400; faze2 = faze2 + 25)
        {


            for (int i = 1; i < 65; i++)
            {
                for (int j = 1; j < 3; j++)
                {
                    //Transform cube;


                    var obj2 = Instantiate(tree) as Transform;


                    var num1 = Random.Range(7, 14);
                    var num2 = Random.Range(8, 16);
                    //basic objects like the cubes will be more frequent
                    // one of a kind objects will have their own positions given
                    obj2.transform.position = new Vector3(((i * num1)-100), 0, (700+((j * num2)) + faze2));
                    obj2.gameObject.GetComponent<Renderer>().material.color = new Color(0.427451f, 0.321569f, 0.176471f);
                    // brownish .445,.27,.075
                    // 0.427451 0.321569 0.176471
                }
            }
        }
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
