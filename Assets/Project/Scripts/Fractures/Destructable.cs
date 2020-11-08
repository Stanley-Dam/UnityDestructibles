using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Fractures {
    public class Destructable : MonoBehaviour {

        public Rigidbody rigidBody;
        [SerializeField] private float internalStrength = 100f;
        [SerializeField] private float externalStrength = 10f;

        /// <summary>
        /// Switch to chunk prefab when a collision with enough velocity happens.
        /// </summary>
        /// <param name="collision">The collision data</param>
        private void OnCollisionEnter(Collision collision) {
            if (collision.relativeVelocity.magnitude > externalStrength) {
                if (transform.childCount > 0) {
                    if (transform.GetChild(0).childCount > 0) {
                        this.gameObject.SetActive(false);
                        GameObject chunks = transform.GetChild(0).gameObject;

                        //chunks.SetActive(true);
                        chunks.transform.SetParent(this.transform.parent, true);

                        for (int i = 0; i < chunks.transform.childCount; i++) {
                            GameObject currentChild = chunks.transform.GetChild(i).gameObject;
                            FractureUtils.ConnectTouchingChunks(currentChild, internalStrength);
                            currentChild.GetComponent<MeshRenderer>().enabled = true;
                            currentChild.GetComponent<MeshCollider>().enabled = true;
                            currentChild.GetComponent<Rigidbody>().isKinematic = false;
                        }

                        ChunkGraphManager graphManager = chunks.AddComponent<ChunkGraphManager>();
                        graphManager.Setup(chunks.GetComponentsInChildren<Rigidbody>());
                    }
                }
            }
        }

    }
}
