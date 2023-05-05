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
    internal class Hitpoints : MonoBehaviour
    {
        public float hitpoints;
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

                int randomInt = UnityEngine.Random.Range(0, 11);
                if (randomInt <= 3) 
                {
                    return;
                }

                Hitpoints hitpoints = door.gameObject.GetOrAddComponent<Hitpoints>();
                BallisticCollider ballsCol = door.GetComponentInChildren<BallisticCollider>();
                if (ballsCol.TypeOfMaterial == MaterialType.MetalThick || ballsCol.TypeOfMaterial == MaterialType.MetalThin)
                {
                    hitpoints.hitpoints = UnityEngine.Random.Range(400, 600);
                }
                else
                {
                    hitpoints.hitpoints = UnityEngine.Random.Range(200, 400);
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
