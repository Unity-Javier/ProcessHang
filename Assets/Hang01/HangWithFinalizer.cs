using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HangWithFinalizer : MonoBehaviour
{
    public ClassWithFinalizer classWithFinalizer;

    public void Start()
    {
        classWithFinalizer = new ClassWithFinalizer("this is the data");
        Debug.Log("Created class with finalizer");  
    }
}
