using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class carema : MonoBehaviour
{
    [SerializeField, Range(1, 50)] private float posSpeed = 20;
    [SerializeField, Range(1, 50)] private float rotSpeed = 20;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(GameObject.Find("Text"));
    }

    private Vector3 _pos = Vector3.zero, _rot = Vector3.zero;
    // Update is called once per frame
    void Update()
    {
        _pos = _rot = Vector3.zero;
        if (Input.GetKey(KeyCode.A)) _pos.x -= 1;
        if (Input.GetKey(KeyCode.D)) _pos.x += 1;
        if (Input.GetKey(KeyCode.S)) _pos.z -= 1;
        if (Input.GetKey(KeyCode.W)) _pos.z += 1;
        if (Input.GetKey(KeyCode.E)) _pos.y -= 1;
        if (Input.GetKey(KeyCode.Q)) _pos.y += 1;
        transform.Translate(_pos*posSpeed*Time.deltaTime,Space.Self);
        
        if (Input.GetKey(KeyCode.UpArrow)) _rot.x -= 1;
        if (Input.GetKey(KeyCode.DownArrow)) _rot.x += 1;
        if (Input.GetKey(KeyCode.RightArrow)) _rot.y += 1;
        if (Input.GetKey(KeyCode.LeftArrow)) _rot.y -= 1;
        transform.Rotate(_rot*rotSpeed*Time.deltaTime,Space.Self);
        if (transform.localEulerAngles.z != 0) transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, 0);
    }
}
