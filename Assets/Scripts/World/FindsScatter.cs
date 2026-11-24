using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// At the start of every run: shuffles all RandomFind objects onto the FindSpots (ground finds onto
/// ground spots, tree finds onto tree spots). There are more spots than finds, so every run looks different.
/// Finds without a free spot are switched off.
/// </summary>
public class FindsScatter : MonoBehaviour
{
    void Awake()
    {
        var spots = new List<FindSpot>(FindObjectsByType<FindSpot>(FindObjectsSortMode.None));
        var finds = new List<RandomFind>(FindObjectsByType<RandomFind>(FindObjectsSortMode.None));

        foreach (bool tree in new[] { false, true })
        {
            var spotList = spots.FindAll(s => s.inTree == tree);
            var findList = finds.FindAll(f => f.inTree == tree);
            Shuffle(spotList);
            for (int i = 0; i < findList.Count; i++)
            {
                var f = findList[i];
                if (i >= spotList.Count)
                {
                    f.gameObject.SetActive(false);
                    if (f.attached != null) f.attached.gameObject.SetActive(false);
                    continue;
                }
                Vector3 p = spotList[i].transform.position;
                f.transform.position = new Vector3(p.x, p.y, f.transform.position.z);
                if (f.attached != null) f.attached.position = f.transform.position + f.attachedOffset;
            }
        }
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
