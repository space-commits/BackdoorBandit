using Comfort.Common;
using DoorBreach;
using EFT.Interactive;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT.Ballistics;

namespace DoorBreach
{
    //create hitpoints unityengine.component
    internal class Breaching : MonoBehaviour
    {
        public float hitpoints;
        public bool canBeShot;
    }

    public class DoorBreachComponent : MonoBehaviour
    { //also all stolen from drakiaxyz

        public void Awake()
        {

            FindObjectsOfType<Door>().ExecuteForEach(door =>
            {
                // We don't support non-operatable doors
                if (!door.Operatable)
                {
                    return;
                }

                // We don't support doors that aren't on the "Interactive" layer
                if (door.gameObject.layer != DoorBreachPlugin.interactiveLayer)
                {
                    return;
                }

                Breaching breachingComponent = door.gameObject.GetOrAddComponent<Breaching>();
                BallisticCollider ballsCol = door.GetComponentInChildren<BallisticCollider>();

                int randomInt = UnityEngine.Random.Range(0, 11);
                if (randomInt <= 4)
                {
                    breachingComponent.canBeShot = false;
                }
                else 
                {
                    breachingComponent.canBeShot = true;
                }
                if (ballsCol.TypeOfMaterial == MaterialType.MetalThick || ballsCol.TypeOfMaterial == MaterialType.MetalThin)
                {
                    breachingComponent.hitpoints = UnityEngine.Random.Range(450, 650);
                }
                else
                {
                    breachingComponent.hitpoints = UnityEngine.Random.Range(250, 450);
                }

                door.OnEnable();

            });
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<DoorBreachComponent>();
            }
        }
    }
}
