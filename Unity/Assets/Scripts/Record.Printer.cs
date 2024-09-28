
using UnityEngine;

public static partial class Recorder
{
    public static void Show(string param)
    {
        Debug.Log("last info :");
        if (LastInfo != null)
            Debug.Log(LastInfo);

        Debug.Log("current info :");
        if (CurrentInfo != null)
            Debug.Log(CurrentInfo);
    }
}
