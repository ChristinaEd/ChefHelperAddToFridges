using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using static StardewValley.Menus.ItemGrabMenu;

namespace ChefHelperAddToFridges.AddToFridges
{
    class AddToFridgesHandler
    {
        private ModEntry modEntry;
        private ClickableTextureComponent button;
        private string hoverText;
        internal StardewValley.GameLocation currentLocation;
        private Texture2D image;
        private Texture2D imageDisabled;

        public AddToFridgesHandler(ModEntry modEntry, Texture2D image, Texture2D imageDisabled)
        {
            this.modEntry = modEntry;
            this.image = image;
            this.imageDisabled = imageDisabled;
            modEntry.Monitor.Log($"Handler created.");
            button = new ClickableTextureComponent(Rectangle.Empty, null, new Rectangle(0, 0, 16, 16), 4f);

            button.hoverText = "Add To Existing Stacks - All Fridges";
        }

        private void UpdatePos()
        {
            // Fill Stacks Button Bounds
            // new Rectangle(xPositionOnScreen + width, yPositionOnScreen + height / 3 - 64 - 64 - 16, 64, 64)

            var menu = Game1.activeClickableMenu;
            if (menu == null) return;

            var length = 16 * Game1.pixelZoom;
            const int positionFromBottom = 3;
            const int gapSize = 16;

            var screenX = menu.xPositionOnScreen + menu.width;
            var screenY = menu.yPositionOnScreen + menu.height / 3 - (length * positionFromBottom) - (gapSize * (positionFromBottom - 1));

            button.bounds = new Rectangle(screenX, screenY, length, length);
        }

        internal bool FridgesAreFree()
        {
            if (currentLocation == null)
                return false;

            if (currentLocation is StardewValley.Locations.FarmHouse)
            {
                var farmHouse = currentLocation as StardewValley.Locations.FarmHouse;

                if (farmHouse.fridge.Value != null && farmHouse.fridge.Value.mutex.IsLocked() && !farmHouse.fridge.Value.mutex.IsLockHeld())
                    return false;
            }

            foreach (StardewValley.Object item in currentLocation.objects.Values)
            {
                if (item != null && item is Chest)
                {
                    var chest = item as Chest;
                    if (chest.fridge.Value)
                    {
                        if (chest.mutex.IsLocked() && !chest.mutex.IsLockHeld())
                            return false;
                    }
                }
            }

            return true;
        }

        public void DrawButton()
        {
            UpdatePos();

            //modEntry.Monitor.Log($"In DrawButton() - Before button.draw.");

            button.texture = FridgesAreFree() ? image : imageDisabled;
            button.draw(Game1.spriteBatch);

            if (hoverText != "")
                IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.smallFont);

            //modEntry.Monitor.Log($"In DrawButton() - Before button.draw.");

            // Draws cursor over the GUI element
            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()),
            Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16), Color.White, 0f, Vector2.Zero,
            4f + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 0);
        }

        internal bool TryHover(float x, float y)
        {
            this.hoverText = "";
            var menu = modEntry.returnFridgeItemGrabMenu();

            if (menu != null)
            {
                if (FridgesAreFree())
                    button.tryHover((int)x, (int)y, 0.25f);

                if (button.containsPoint((int)x, (int)y))
                {
                    this.hoverText = FridgesAreFree() ? button.hoverText : "Disabled - Fridge(s) in Use";
                    menu.hoveredItem = null;
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Modified version of game's ItemGrabMenu.FillOutStacks().
        /// Works with any given chest instead of just the one
        /// the Farmer is currently interacting with.
        /// </summary>
        /// <param name="chest"></param>
        public void FillOutStacks(Chest chest)
        {
            var menu = modEntry.returnFridgeItemGrabMenu();
            var inventory = modEntry.returnFridgeItemGrabMenu().inventory;

            for (int i = 0; i < chest.items.Count; i++)
            {
                Item chest_item = chest.items[i];
                if (chest_item == null || chest_item.maximumStackSize() <= 1)
                {
                    continue;
                }
                for (int j = 0; j < inventory.actualInventory.Count; j++)
                {
                    Item inventory_item = inventory.actualInventory[j];
                    if (inventory_item == null || !chest_item.canStackWith(inventory_item))
                    {
                        continue;
                    }
                    TransferredItemSprite item_sprite = new TransferredItemSprite(inventory_item.getOne(), inventory.inventory[j].bounds.X, inventory.inventory[j].bounds.Y);
                    var transferredItemSprites = modEntry.helper.Reflection.GetField<List<TransferredItemSprite>>(menu, "_transferredItemSprites").GetValue();
                    transferredItemSprites.Add(item_sprite);
                    int stack_count2 = inventory_item.Stack;
                    if (chest_item.getRemainingStackSpace() > 0)
                    {
                        stack_count2 = chest_item.addToStack(inventory_item);
                        menu.ItemsToGrabMenu?.ShakeItem(chest_item);
                    }
                    inventory_item.Stack = stack_count2;
                    while (inventory_item.Stack > 0)
                    {
                        Item overflow_stack = null;
                        if (!Utility.canItemBeAddedToThisInventoryList(chest_item.getOne(), chest.items, Chest.capacity))
                        {
                            break;
                        }
                        if (overflow_stack == null)
                        {
                            for (int l = 0; l < chest.items.Count; l++)
                            {
                                if (chest.items[l] != null && chest.items[l].canStackWith(chest_item) && chest.items[l].getRemainingStackSpace() > 0)
                                {
                                    overflow_stack = chest.items[l];
                                    break;
                                }
                            }
                        }
                        if (overflow_stack == null)
                        {
                            for (int k = 0; k < chest.items.Count; k++)
                            {
                                if (chest.items[k] == null)
                                {
                                    Item item = chest.items[k] = chest_item.getOne();
                                    overflow_stack = item;
                                    overflow_stack.Stack = 0;
                                    break;
                                }
                            }
                        }
                        if (overflow_stack == null && chest.items.Count < Chest.capacity)
                        {
                            overflow_stack = chest_item.getOne();
                            overflow_stack.Stack = 0;
                            chest.items.Add(overflow_stack);
                        }
                        if (overflow_stack == null)
                        {
                            break;
                        }
                        stack_count2 = overflow_stack.addToStack(inventory_item);
                        menu.ItemsToGrabMenu?.ShakeItem(chest_item);
                        inventory_item.Stack = stack_count2;
                    }
                    if (inventory_item.Stack == 0)
                    {
                        inventory.actualInventory[j] = null;
                    }
                }
            }
        }

        private void fillFridges()
        {
            // Fill main fridge first
            if (currentLocation is StardewValley.Locations.FarmHouse) {
                var farmHouse = currentLocation as StardewValley.Locations.FarmHouse;

                if (farmHouse.fridge.Value != null)
                    FillOutStacks(farmHouse.fridge.Value);
            }

            // Then fill all mini-fridges
            foreach (StardewValley.Object item in currentLocation.objects.Values)
            {
                if (item != null && item is Chest) {
                    var chest = item as Chest;
                    if (chest.fridge.Value) {
                        FillOutStacks(chest);
                        //fridges.Add(item as Chest);
                    }
                }
            }
        }

        internal void HandleClick(ICursorPosition cursor)
        {
            /*var chest = GetOpenChest();
            if (chest == null) return;*/

            var screenPixels = cursor.ScreenPixels;

            if (!button.containsPoint((int)screenPixels.X, (int)screenPixels.Y)) return;

            if (FridgesAreFree())
                Game1.playSound("Ship");

            fillFridges();

            //menu.FillOutStacks();

            /*if (Chests.Contains(chest))
            {
                Chests.Remove(chest);
            }
            else
            {
                Chests.Add(chest);
            }*/
        }
    }
}
