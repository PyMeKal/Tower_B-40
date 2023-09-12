using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[SerializeField]
public class PFNode
{
    public Vector2 position;
    public PFNode[] adjacentNodes;
}

public class PFNodeInterface : MonoBehaviour
{
    public PFNode node = new PFNode();

    public List<Transform> adjacentNodeTransforms;
    
    // Start is called before the first frame update
    void Start()
    {
        node.position = transform.position;
        node.adjacentNodes = adjacentNodeTransforms.Select(t => t.GetComponent<PFNodeInterface>().node).ToArray();
    }
}
