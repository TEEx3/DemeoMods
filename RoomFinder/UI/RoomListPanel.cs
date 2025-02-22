﻿namespace RoomFinder.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Boardgame;
    using Common.UI;
    using HarmonyLib;
    using Photon.Realtime;
    using UnityEngine;
    using Object = UnityEngine.Object;

    internal class RoomListPanel
    {
        private readonly UiHelper _uiHelper;
        private Func<RoomListEntry, object> _sortOrder;
        private bool _isDescendingOrder;
        private List<RoomListEntry> _rooms;

        internal GameObject GameObject { get; }

        internal static RoomListPanel NewInstance(UiHelper uiHelper)
        {
            return new RoomListPanel(uiHelper, new GameObject("RoomListPanel"));
        }

        private RoomListPanel(UiHelper uiHelper, GameObject panel)
        {
            this._uiHelper = uiHelper;
            this.GameObject = panel;
            this._sortOrder = r => r.CurrentPlayers;
            this._isDescendingOrder = true;

            this._rooms = new List<RoomListEntry>();
        }

        internal void SetRooms(IEnumerable<RoomInfo> rooms)
        {
            _rooms = rooms.Select(RoomListEntry.Parse).ToList();
            SortRooms();
            Render();
        }

        private void Render()
        {
            foreach (Transform child in GameObject.transform)
            {
                Object.Destroy(child.gameObject);
            }

            var header = CreateHeader();
            header.transform.SetParent(GameObject.transform, worldPositionStays: false);

            var rooms = new GameObject("Rooms");
            rooms.transform.SetParent(GameObject.transform, worldPositionStays: false);

            var roomIndex = 0;
            foreach (var room in _rooms)
            {
                if (room.GameType == LevelSequence.GameType.Invalid)
                {
                    continue;
                }

                if (room.Floor < 0)
                {
                    continue;
                }

                var yOffset = (1 + roomIndex++) * -1f;
                var roomRow = CreateRoomRow(room);
                roomRow.transform.SetParent(rooms.transform, worldPositionStays: false);
                roomRow.transform.localPosition = new Vector3(0, yOffset, 0);
            }
        }

        private GameObject CreateHeader()
        {
            var container = new GameObject("Header");

            var sortLabel = _uiHelper.CreateLabelText("Sort by:");
            sortLabel.transform.SetParent(container.transform, worldPositionStays: false);
            sortLabel.transform.localPosition = new Vector3(-3f, 0, UiHelper.DefaultTextZShift);

            var gameButton = CreateSortButton("Game", () => SetSortOrderAndApply(r => r.GameType));
            gameButton.transform.SetParent(container.transform, worldPositionStays: false);
            gameButton.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            gameButton.transform.localPosition = new Vector3(-0.5f, 0, 0);

            var floorButton = CreateSortButton("Floor", () => SetSortOrderAndApply(r => r.Floor));
            floorButton.transform.SetParent(container.transform, worldPositionStays: false);
            floorButton.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            floorButton.transform.localPosition = new Vector3(1.5f, 0, 0);

            var playersButton = CreateSortButton("Players", () => SetSortOrderAndApply(r => r.CurrentPlayers));
            playersButton.transform.SetParent(container.transform, worldPositionStays: false);
            playersButton.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            playersButton.transform.localPosition = new Vector3(3.5f, 0, 0);

            return container;
        }

        private GameObject CreateSortButton(string text, Action action)
        {
            var container = new GameObject(text);

            var button = _uiHelper.CreateButton(action);
            button.transform.SetParent(container.transform, worldPositionStays: false);
            button.transform.localScale = new Vector3(0.75f, 1, 1);

            var buttonText = _uiHelper.CreateText(text, Color.white, UiHelper.DefaultButtonFontSize);
            buttonText.transform.SetParent(container.transform, worldPositionStays: false);
            buttonText.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            buttonText.transform.localPosition = new Vector3(0, 0, UiHelper.DefaultTextZShift);

            return container;
        }

        private void SetSortOrderAndApply(Func<RoomListEntry, object> sortOrder)
        {
            if (_sortOrder == sortOrder)
            {
                _isDescendingOrder = !_isDescendingOrder;
            }

            _sortOrder = sortOrder;
            SortRooms();
            Render();
        }

        private void SortRooms()
        {
            _rooms = _isDescendingOrder
                ? _rooms.OrderByDescending(_sortOrder).ToList()
                : _rooms.OrderBy(_sortOrder).ToList();
        }

        private GameObject CreateRoomRow(RoomListEntry room)
        {
            var container = new GameObject(room.Name);

            var joinButton = _uiHelper.CreateButton(JoinRoomAction(room.Name));
            joinButton.transform.SetParent(container.transform, worldPositionStays: false);
            joinButton.transform.localScale = new Vector3(0.4f, 0.7f, 0.7f);
            joinButton.transform.localPosition = new Vector3(-3f, 0, 0);

            var joinText = _uiHelper.CreateText(room.Name, Color.white, UiHelper.DefaultLabelFontSize);
            joinText.transform.SetParent(container.transform, worldPositionStays: false);
            joinText.transform.localPosition = new Vector3(-3f, 0, UiHelper.DefaultTextZShift);

            var gameLabel = _uiHelper.CreateLabelText(room.GameType.ToString());
            gameLabel.transform.SetParent(container.transform, worldPositionStays: false);
            gameLabel.transform.localPosition = new Vector3(-0.4f, 0, UiHelper.DefaultTextZShift);

            var floorLabel = _uiHelper.CreateLabelText(room.Floor.ToString());
            floorLabel.transform.SetParent(container.transform, worldPositionStays: false);
            floorLabel.transform.localPosition = new Vector3(1.75f, 0, UiHelper.DefaultTextZShift);

            var playersLabel = _uiHelper.CreateLabelText($"{room.CurrentPlayers}/{room.MaxPlayers}");
            playersLabel.transform.SetParent(container.transform, worldPositionStays: false);
            playersLabel.transform.localPosition = new Vector3(3.25f, 0, UiHelper.DefaultTextZShift);

            return container;
        }

        private static Action JoinRoomAction(string roomCode)
        {
            return () =>
            {
                RoomFinderMod.Logger.Msg($"Joining room [{roomCode}].");
                Traverse.Create(RoomFinderMod.ModState.GameContext.gameStateMachine.lobby.GetLobbyMenuController)
                    .Method("JoinGame", roomCode, true)
                    .GetValue();
            };
        }
    }
}
