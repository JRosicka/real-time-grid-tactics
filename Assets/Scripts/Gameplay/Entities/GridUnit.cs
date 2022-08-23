using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A <see cref="GridEntity"/> that represents a unit that can move around and attack or build stuff. 
/// </summary>
public class GridUnit : GridEntity {
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override bool CanTargetEntities() {
        return true;
    }
}
