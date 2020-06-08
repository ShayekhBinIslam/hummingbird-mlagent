using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerArea : MonoBehaviour
{
    // The diameter of the area for distance
    public const float AreaDiameter = 20f;

    // list of all flower plants
    private List<GameObject> flowerPlants;

    // lookup for flower from a nectar collider
    private Dictionary<Collider, Flower> nectarFlowerDictionary;

    /// <summary>
    /// list of all flowers
    /// </summary>
    public List<Flower> Flowers { get; private set; }

    /// <summary>
    /// Reset flowers and plants
    /// </summary>
    public void ResetFlowers()
    {
        // rotate around y, some x, z
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        foreach (Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }
    
    /// <summary>
    /// Gets the Flower that a nectar collider belong
    /// </summary>
    /// <param name="collider">nectar collider</param>
    /// <returns>The matching flower</returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }

    /// <summary>
    /// Called when area wake up
    /// </summary>
    public void Awake()
    {
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
        FindChildFlowers(transform);
    }

    // called when the game starts
    private void Start()
    {
        // find all flowers that are childrens of GameObject
        
    }

    /// <summary>
    /// find all parents
    /// </summary>
    /// <param name="parent">parents of the children to check</param>
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.CompareTag("flower_plant"))
            {
                // 
                flowerPlants.Add(child.gameObject);
                FindChildFlowers(child);
            }
            else
            {
                // not a flower plant look for 
                Flower flower = child.GetComponent<Flower>();

                if (flower != null)
                {
                    Flowers.Add(flower);
                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);
                }
                else
                {
                    FindChildFlowers(child);
                }
            }

        }
    }
}
