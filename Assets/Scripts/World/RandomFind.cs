using UnityEngine;

/// <summary>Something you can find (junk, useful items, clothes). It is moved to a random matching FindSpot each run.</summary>
public class RandomFind : MonoBehaviour
{
    public bool inTree;
    [Tooltip("A decoration that moves with it (the cloth scrap in a tree).")]
    public Transform attached;
    public Vector3 attachedOffset;
}
