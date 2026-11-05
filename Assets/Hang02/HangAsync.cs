using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class ExamplePlugin : MonoBehaviour
{
    // Import the functions from the shared library
    [DllImport("hangingplugin")]
    private static extern void HelloFromCpp();

    [DllImport("hangingplugin")]
    private static extern void Hang();

    public Task<int> RunLongRunningTaskAsync()
    {
        return Task.Run(() =>
        {
            Hang();
            return 1;
        });
    }
    
    public async void Start()
    {
        Debug.Log("Process will hang");
        await RunLongRunningTaskAsync();
    }
}