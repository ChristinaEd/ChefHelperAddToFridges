using System;
using ChefHelperAddToFridges.AddToFridges;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ChefHelperAddToFridges
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private AddToFridgesHandler handler;
        internal IModHelper helper;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Assets
            var button = helper.Content.Load<Texture2D>("assets/button.png");
            var buttonDisabled = helper.Content.Load<Texture2D>("assets/button_disabled.png");

            this.handler = new AddToFridgesHandler(this, button, buttonDisabled);
            this.helper = helper;

            AddEvents(helper);
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Adds Events to the SMAPI helper.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        private void AddEvents(IModHelper helper)
        {
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.CursorMoved += OnCursorMoved;
        }

        internal ItemGrabMenu ReturnFridgeItemGrabMenu()
        {
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is ItemGrabMenu)
            {
                var menu = Game1.activeClickableMenu as ItemGrabMenu;
                if (menu.behaviorOnItemGrab?.Target is Chest chest && chest.fridge.Value)
                    return menu;
            }
            return null;
        }

        private void OnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            int x = (int)e.NewPosition.ScreenPixels.X;
            int y = (int)e.NewPosition.ScreenPixels.Y;
            if (handler.TryHover(x, y))
            {
                MouseState cur = Game1.oldMouseState;
                Game1.oldMouseState = new MouseState(
                    x: x,
                    y: y,
                    scrollWheel: cur.ScrollWheelValue,
                    leftButton: cur.LeftButton,
                    middleButton: cur.MiddleButton,
                    rightButton: cur.RightButton,
                    xButton1: cur.XButton1,
                    xButton2: cur.XButton2
                );
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            var menu = ReturnFridgeItemGrabMenu();
            if (menu != null && e.Button == SButton.MouseLeft && handler.FridgesAreFree())
            {
                handler.HandleClick(e.Cursor);
            }
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (ReturnFridgeItemGrabMenu() != null)
            {
                handler.currentLocation = Game1.player.currentLocation;
                handler.DrawButton();
            } 
            else
            {
                handler.currentLocation = null;
            }

            /*if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is ItemGrabMenu)
            {
                var menu = Game1.activeClickableMenu as ItemGrabMenu;
                if (menu.behaviorOnItemGrab?.Target is Chest chest && chest.fridge.Value)
                    handler.DrawButton();
            }*/
        }
    }
}