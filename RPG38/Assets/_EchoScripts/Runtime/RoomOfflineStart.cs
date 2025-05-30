using System;
using JKFrame;
using UnityEngine;

namespace GameLogic.Runtime
{
    public class RoomOfflineStart : MonoBehaviour
    {
        private void Start()
        {
            GameHub.InitArchitecture();
            UISystem.Show<LobbyUI>();
        }
    }
}