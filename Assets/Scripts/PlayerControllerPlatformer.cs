using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterControllerPlatformer))]
public class PlayerControllerPlatformer : MonoBehaviour {
    CharacterControllerPlatformer ccp;
	
	void Start ()
    {
        ccp = GetComponent<CharacterControllerPlatformer>();

    }
	
    void FixedUpdate()
    {
        ccp.walk(Input.GetAxisRaw("Horizontal"));
    }

	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyDown("b"))
        {
            ccp.jump();
        }
	}
}
