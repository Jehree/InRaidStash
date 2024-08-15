using Comfort.Common;
using Cutscene;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SPT.Custom.Airdrops.Utils;
using SPT.Reflection.Utils;
using SPT.Common.Http;
using EFT.Hideout;
using JetBrains.Annotations;
using System.Xml.Linq;

namespace InRaidStash.Patches
{
    internal class GameStartedPatch : ModulePatch
    {
        private const string CRATE_ID = "5811ce772459770e9e5f9532";

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;

            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            if (player == null)
            {
                Plugin.LogSource.LogError("Player is null!");
                return;
            }

            GameObject lootableContainerObj = GetLootableContainer("custom_test_container", CRATE_ID);
            lootableContainerObj.transform.position = player.Transform.position;
            LootableContainer lootableContainer = lootableContainerObj.GetComponentInChildren<LootableContainer>();
            
            foreach (GameObject exfilPoint in Plugin.ExfiltrationPointList)
            {
                Plugin.LogSource.LogError(exfilPoint.name);
                AddRemoteInteractableComponent("exfil_stash_access", lootableContainer, exfilPoint);
            }

            GameObject remoteInteractableObj = AddRemoteInteractableComponent("test_remote_interactable", lootableContainer);
            Vector3 remotePos = new Vector3(player.Transform.position.x, player.Transform.position.y + 1.2f, player.Transform.position.z);
            remoteInteractableObj.transform.position = remotePos;

            GameObject remoteInteractableObj2 = AddRemoteInteractableComponent("test_remote_interactable2", lootableContainer);
            Vector3 remotePos2 = new Vector3(player.Transform.position.x, player.Transform.position.y + 1.2f, player.Transform.position.z + 1.2f);
            remoteInteractableObj2.transform.position = remotePos2;


            /*
            Item ai2Item = itemFactory.CreateItem("5755356824597772cb798962", "5755356824597772cb798962", null);
            
            LocationInGrid itemLocation = new LocationInGrid();
            itemLocation.x = 3;

            //lootableContainerComponent.ItemOwner.MainStorage[0].Add(ai2Item, itemLocation, false);
            */

        }

        public static GameObject AddRemoteInteractableComponent(string name, LootableContainer stashContainer, GameObject gameObject=null)
        {
            GameObject obj;
            if (gameObject == null)
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.GetComponent<Renderer>().material.color = Color.blue;
                obj.GetComponent<Renderer>().enabled = true;
                obj.name = name;
                obj.layer = 22; //interactable layer
            }
            else
            {
                obj = gameObject;
            }

            obj.AddComponent<CustomInteractableComponent>();
            var interactableComponent = obj.GetComponent<CustomInteractableComponent>();

            GetActionsClass.Class1517 @class = new GetActionsClass.Class1517();
            var owner = Singleton<GameWorld>.Instance.MainPlayer.GetComponent<GamePlayerOwner>();
            @class.owner = owner;
            @class.container = stashContainer;

            ActionsReturnClass actions = new ActionsReturnClass
            {
                Actions =
                {
                    new ActionsTypesClass
                    {
                        Name = "Open Stash",
                        Action = new Action(@class.method_0)
                    }
                }
            };
            
            interactableComponent.Actions = actions;
            return obj;
        }

        public static GameObject GetLootableContainer(string name, string crateItemId)
        {
            GameObject parentObj = new GameObject();
            parentObj.name = $"{name}_parent";

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.parent = parentObj.transform;
            obj.GetComponent<Renderer>().material.color = Color.red;
            obj.GetComponent<Renderer>().enabled = true;
            obj.name = name;
            obj.layer = 22; //interactable layer
            obj.AddComponent<LootableContainer>();
            LootableContainer containerComponent = obj.GetComponent<LootableContainer>();

            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
            Item crateItem = itemFactory.CreateItem(crateItemId, crateItemId, null);
            LootItem.CreateLootContainer(containerComponent, crateItem, name, Singleton<GameWorld>.Instance);

            containerComponent.Id = $"{name}_id";

            var initialSyncStateSetter = new WorldInteractiveObject.GStruct384($"{name}_id", EDoorState.Shut, 0);
            containerComponent.SetInitialSyncState(initialSyncStateSetter);

            containerComponent.OpenSound = Array.Empty<AudioClip>();
            containerComponent.ShutSound = Array.Empty<AudioClip>();
            containerComponent.SqueakSound = Array.Empty<AudioClip>();

            return parentObj;
        }
    }
}
