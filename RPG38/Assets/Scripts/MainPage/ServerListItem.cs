// ServerListItem.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;        // 确保有这行

public class ServerListItem : MonoBehaviour
{
    public TMP_Text addressText;   // TextMeshPro 文本
    public Button joinButton;      // 按钮组件

    public string Address { get; private set; }

    public void Setup(string address, System.Action onJoin)
    {
        Address = address;
        addressText.text = address; // 给 TMP_Text 赋值
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => onJoin());
    }
}