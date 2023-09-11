using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[SerializeField]
public struct PFNode
{
    public Vector2 position;
    public PFNode[] adjacentNodes;
}

public class PFNodeInterface : MonoBehaviour
{
    private PFNode node;

    public List<Transform> adjacentNodeTransforms;
    
    // Start is called before the first frame update
    void Start()
    {
        node.position = transform.position;
        node.adjacentNodes = adjacentNodeTransforms.Select(t => t.GetComponent<PFNodeInterface>().node).ToArray();
    }
}
