using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterControllerPlatformer))]
public class PlayerControllerPlatformer : MonoBehaviour {
    CharacterControllerPlatformer ccp;

    float movementThreshold = .2f;
	
	void Start ()
    {
        ccp = GetComponent<CharacterControllerPlatformer>();

    }
	
    void FixedUpdate()
    {
        var horizontalMovement = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(horizontalMovement) > movementThreshold)
            ccp.walk(horizontalMovement);
    }

	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyDown("space"))
            ccp.jump();
        if (Input.GetKey("space")) ccp.tryUp();
        if (Input.GetMouseButtonDown(0))
        {
            var target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            target.z = 0;
            ccp.shootGrapple(target);
        }
        if (Input.GetMouseButtonUp(0)) ccp.releaseGrapple();

    }
}
