using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{

    private GameObject cam;
    [SerializeField] private float parallaxEffect;

    private float xPosition;

    private float length;
    private float size;
    
    
    // Start is called before the first frame update
    void Start()
    {
        cam = GameObject.Find("Main Camera");
        
        length = 0;
        size = GetComponent<SpriteRenderer>().bounds.size.x;
        
        xPosition = transform.position.x;
    }

    // Update is called once per frame
    void Update()
    {
        float distanceMoved = transform.position.x * (1-parallaxEffect);
        float distanceToMove = cam.transform.position.x * parallaxEffect;
        
        transform.position = new Vector3(xPosition + distanceToMove+2*size, transform.position.y);
        
        if(distanceMoved >xPosition + length)
            xPosition += length;
        else if (distanceToMove < xPosition - length)
            xPosition -= length;
        
    }
}
