using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public TransformCurve aliveCurve;
    public TransformCurve deadCurve;
    bool _isAlive = true;
    public bool isAlive
    {
        get => _isAlive;
        set
        {
            aliveCurve.enabled = value;
            deadCurve.enabled = !value;
            _isAlive = value;
        }

    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }
}
