using UnityEngine;

public class InfiniteSpriteBG : MonoBehaviour
{
    [Tooltip("主摄像机或玩家 Transform，用于检测移动方向")]
    public Transform target;

    [Tooltip("单个背景片段的宽度（世界单位），和你摆放时的 segmentWidth 一致")]
    public float segmentWidth = 20f;

    [Tooltip("场景中一开始并排的背景片段，顺序：0=最左, 1=中间, 2=最右")]
    public Transform[] segments = new Transform[3];

    private int leftIndex  = 0;
    private int rightIndex = 2;

    void LateUpdate()
    {
        if (target == null) return;

        float camX = target.position.x;

        // 当相机移动到“右段”中心右侧，就把最左段移到最右
        if (camX > segments[rightIndex].position.x + segmentWidth * 0.5f)
            ScrollRight();

        // 当相机移动到“左段”中心左侧，就把最右段移到最左
        else if (camX < segments[leftIndex].position.x - segmentWidth * 0.5f)
            ScrollLeft();
    }

    private void ScrollRight()
    {
        // 把最左段挪到最右端
        Transform leftSeg  = segments[leftIndex];
        Transform rightSeg = segments[rightIndex];

        leftSeg.position = new Vector3(
            rightSeg.position.x + segmentWidth,
            leftSeg.position.y,
            leftSeg.position.z
        );

        // 更新索引：左→中→右
        rightIndex = leftIndex;
        leftIndex  = (leftIndex + 1) % segments.Length;
    }

    private void ScrollLeft()
    {
        // 把最右段挪到最左端
        Transform leftSeg  = segments[leftIndex];
        Transform rightSeg = segments[rightIndex];

        rightSeg.position = new Vector3(
            leftSeg.position.x - segmentWidth,
            rightSeg.position.y,
            rightSeg.position.z
        );

        // 更新索引：右→中→左
        leftIndex  = rightIndex;
        rightIndex = (rightIndex - 1 + segments.Length) % segments.Length;
    }
    
    
}