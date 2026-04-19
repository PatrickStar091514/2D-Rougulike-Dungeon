using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueDungeon.Tests
{
    public class ShowTriggerPos : MonoBehaviour
    {

        public Collider2D triggerCollider;
        // Start is called before the first frame update
        void Start()
        {
            triggerCollider = GetComponent<Collider2D>();
        
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log($"Trigger Position: {triggerCollider.bounds.center}");
        }
    }
}
