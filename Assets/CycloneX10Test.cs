using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CycloneX10))]
public class CycloneX10Test : MonoBehaviour {

    void OnGUI()
    {
        CycloneX10 cycloneX10 = GetComponent<CycloneX10>();

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));

        GUILayout.Label("Pattern");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < 10; i++)
        {
            if (GUILayout.Toggle(i == cycloneX10.pattern, i.ToString()))
            {
                cycloneX10.pattern = i;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Level");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < 10; i++)
        {
            if (GUILayout.Toggle(i == cycloneX10.level, i.ToString()))
            {
                cycloneX10.level = i;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

}
