using System;
using JKFrame;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLogic.Runtime
{
    public class GameStart : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
            SceneManager.LoadScene("_EchoAddressable/Scenes/RoomOffline");
        }
    }
}