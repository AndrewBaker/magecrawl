﻿using System;
using System.Collections.Generic;
using Magecrawl.GameEngine.Interfaces;

namespace Magecrawl.Keyboard.Inventory
{
    internal sealed class InventoryItemKeyboardHandler : BaseKeystrokeHandler
    {
        private IGameEngine m_engine;
        private GameInstance m_gameInstance;

        public InventoryItemKeyboardHandler(IGameEngine engine, GameInstance instance)
        {
            m_engine = engine;
            m_gameInstance = instance;
        }

        public override void NowPrimaried()
        {
            m_gameInstance.UpdatePainters();
        }

        private void Select()
        {
            m_gameInstance.SendPaintersRequest("InventoryItemOptionSelected", new Magecrawl.GameUI.Inventory.InventoryItemOptionSelected(InventoryItemOptionSelectedDelegate));
        }

        private void InventoryItemOptionSelectedDelegate(string optionName)
        {
            m_gameInstance.TextBox.AddText(optionName);
            m_gameInstance.SendPaintersRequest("StopShowingInventoryItemWindow");
            m_gameInstance.UpdatePainters();
            m_gameInstance.ResetHandlerName();
        }

        private void Escape()
        {
            m_gameInstance.SendPaintersRequest("StopShowingInventoryItemWindow");
            
            // We're about to reshow the inventory window, don't reset the position then
            m_gameInstance.SendPaintersRequest("InventoryWindowSavePositionForNextShow");
            m_gameInstance.UpdatePainters();
            m_gameInstance.SetHandlerName("Inventory");
        }

        private void HandleDirection(Direction direction)
        {
            m_gameInstance.SendPaintersRequest("InventoryItemPositionChanged", direction);
            m_gameInstance.UpdatePainters();
        }

        private void North()
        {
            HandleDirection(Direction.North);
        }

        private void South()
        {
            HandleDirection(Direction.South);
        }
    }
}
