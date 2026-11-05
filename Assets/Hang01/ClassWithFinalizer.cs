using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

public class ClassWithFinalizer
{
    public string dataForFinalizer;

    public ClassWithFinalizer(string data)
    {
        dataForFinalizer = data; 
    }

    ~ClassWithFinalizer()
    {
        /*dataForFinalizer = null;
        Debug.Log("Finalizer called");
        
        using (HttpClient client = new HttpClient())
        {
            var result =  client.GetStringAsync("https://www.google.com").Result;
            Console.WriteLine("Website content fetched in finalizer (dispose): " + result.Substring(0, 50)); // Print first 50 chars
        }*/
    }
}
