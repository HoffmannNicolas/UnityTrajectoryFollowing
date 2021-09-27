using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameObjectPooler {

    /* Stores and manages GameObject instances to be used and reused instead of destroyed and created anew */

    private List<GameObject> objects = new List<GameObject>();
    private GameObject holder; // A place to store all objects cleanly
    private GameObject prefab; // Objects needed to be managed

    public GameObjectPooler(GameObject _prefab, string name) {
        this.holder = new GameObject(name);
        this.prefab = _prefab;
    }

    public void free(GameObject obj) {
        int index = objects.IndexOf(obj); // Identify where the object is stored
        if (index == -1) // Object not found within objects
            Debug.Log("[Warning] :: [Object Pooler] :: Trying to free an object that did not belong here.");
        else
            objects[index].SetActive(false); // Find it, free it
    }

    public GameObject get() {
        int index = this.objects.IndexOf(this.objects.Where(x => !x.activeSelf).FirstOrDefault()); // Look for an un-used (i.e. inactive) object
        if (index == -1) {  // If no un-used object is found, create a new one
            GameObject newObject = Object.Instantiate(this.prefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
            newObject.name = this.holder.name + "_" + this.objects.Count;
            newObject.transform.parent = this.holder.transform;
            this.objects.Add(newObject);
            return newObject;
        }
        else { // An un-used item is found, return it to be used again
            this.objects[index].SetActive(true);
            return this.objects[index];
        }
    }
}
