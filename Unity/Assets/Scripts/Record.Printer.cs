
using UnityEngine;

public static partial class Recorder
{
    public static void Show(string param)
    {
        Debug.Log("last info :");
        Debug.Log(LastInfo);

        Debug.Log("current info :");
        Debug.Log(CurrentInfo);
    }
}
