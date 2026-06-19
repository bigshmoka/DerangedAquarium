using UnityEngine;

public class AlgaeManager : MonoBehaviour
{
    [Header("Tracked Algae Zones")]
    public AlgaeNode[] algaeNodes; // Drop your individual wall/floor objects here!

    // The snail will call this function to find out where it should go crawl next!
    public AlgaeNode GetDirtiestAlgaeNode()
    {
        AlgaeNode dirtiestNode = null;
        float highestAlgae = 0.05f; // Small threshold so snail doesn't chase 1% dirty spots

        foreach (AlgaeNode node in algaeNodes)
        {
            if (node != null && node.currentAlgaeLevel > highestAlgae)
            {
                highestAlgae = node.currentAlgaeLevel;
                dirtiestNode = node;
            }
        }

        return dirtiestNode; // Returns the worst wall, or null if the tank is sparkling clean
    }
}