using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A <see cref="GridEntity"/> that represents a structure that can build <see cref="GridUnit"/>s or research upgrades.
/// </summary>
public class GridStructure : GridEntity
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override bool CanTargetEntities() {
        return false;
    }
}
