using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Grid))]
public class AStarPathfinder : MonoBehaviour
{
    [Header("格子大小（与 Tilemap 的 Cell Size 一致，一般默认是 1）")]
    public float nodeSize = 1f;

    [Header("表示障碍的 Tilemap 所在的 Layer（名称要与场景里 Tilemap 的 Layer 一致）")]
    public string obstacleLayerName = "Ground";

    [Header("固定网格宽度（格子数），请在 Inspector 中按需设置")]
    public int gridWidth = 300;

    [Header("固定网格高度（格子数），请在 Inspector 中按需设置")]
    public int gridHeight = 200;

    [Header("强制网格左下角的瓦片坐标 X")]
    public int forcedMinX = -101;

    [Header("强制网格左下角的瓦片坐标 Y")]
    public int forcedMinY = -36;

    [Header("Boss 的 Collider2D（拖入怪物的碰撞体）")]
    public Collider2D bossCollider;

    private Vector3 originPosition;   // 网格左下角对应的世界坐标
    private bool[,] walkable;
    private Node[,] nodes;
    private List<Tilemap> obstacleTilemaps = new List<Tilemap>();

    private class Node
    {
        public int x, y;
        public bool walkable;
        public int gCost, hCost;
        public Node parent;
        public int fCost => gCost + hCost;

        public Node(int _x, int _y, bool _walkable)
        {
            x = _x;
            y = _y;
            walkable = _walkable;
            gCost = int.MaxValue;
            hCost = 0;
            parent = null;
        }
    }

    void Awake()
    {
        Debug.Log($"[A*] Awake 被调用，obstacleLayerName = {obstacleLayerName}");
        Debug.Log($"[A*] 固定网格宽度 = {gridWidth}, 固定网格高度 = {gridHeight}");
        Debug.Log($"[A*] 强制网格左下角 = ({forcedMinX}, {forcedMinY})");

        if (bossCollider == null)
        {
            Debug.LogWarning("[A*] bossCollider 未设置！将不会进行碰撞体膨胀处理。");
        }
        else
        {
            Debug.Log($"[A*] 已设置 bossCollider: {bossCollider.name}");
        }

        // 1. 扫描场景中所有 Tilemap，把位于 obstacleLayerName 这一 Layer 的都当作障碍 Tilemap
        obstacleTilemaps.Clear();
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        foreach (Tilemap tm in allTilemaps)
        {
            if (tm.gameObject.layer == LayerMask.NameToLayer(obstacleLayerName))
            {
                obstacleTilemaps.Add(tm);
                Debug.Log($"[A*] 扫描到障碍 Tilemap: {tm.name}");
            }
        }

        if (obstacleTilemaps.Count == 0)
        {
            Debug.LogWarning($"[A*] 没有找到任何属于 Layer “{obstacleLayerName}” 的 Tilemap！");
        }
        else
        {
            Debug.Log($"[A*] 找到 {obstacleTilemaps.Count} 张障碍 Tilemap");
        }

        // 2. 计算网格左下角的世界坐标 & 创建 walkable/nodes 数组
        CalculateGridBounds();
        Debug.Log($"[A*] 计算网格：originPosition = {originPosition}");

        // 3. 构建 walkable 数组并初始化 Node
        BuildWalkableGrid();
    }

    private void CalculateGridBounds()
    {
        Vector3Int forcedMinCell = new Vector3Int(forcedMinX, forcedMinY, 0);
        Grid parentGrid = GetComponent<Grid>();
        originPosition = parentGrid.CellToWorld(forcedMinCell);

        walkable = new bool[gridWidth, gridHeight];
        nodes    = new Node[gridWidth, gridHeight];
    }

    private void BuildWalkableGrid()
    {
        if (obstacleTilemaps.Count == 0)
            return;

        // 1. 初始化为全可走
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                walkable[x, y] = true;
            }
        }

        Grid parentGrid = GetComponent<Grid>();

        // 2. 收集所有原始障碍格子（在 Tilemap 上真的有 Tile 的）
        List<Vector2Int> rawBlockedCells = new List<Vector2Int>();
        foreach (Tilemap tm in obstacleTilemaps)
        {
            BoundsInt bds = tm.cellBounds;
            foreach (Vector3Int cellPos in bds.allPositionsWithin)
            {
                if (!tm.HasTile(cellPos))
                    continue;

                // 计算该瓦片格子中心的世界坐标
                Vector3 cellWorldBL = parentGrid.CellToWorld(cellPos);
                Vector3 worldCenter = cellWorldBL + new Vector3(
                    nodeSize * 0.5f,
                    nodeSize * 0.5f,
                    0f
                );

                if (WorldToGrid(worldCenter, out int xIdx, out int yIdx))
                {
                    rawBlockedCells.Add(new Vector2Int(xIdx, yIdx));
                }
                else
                {
                    Debug.LogWarning($"[A*] worldCenter 超出网格范围，WorldToGrid 返回 false");
                }
            }
        }

        // 3. 如果 bossCollider 设置了，就根据它的 Collider2D 类型计算“膨胀范围（＋1 额外保留）”
//    并额外记录 offset 在世界坐标里的偏移量 worldOffset
int inflateRadiusInNodes = 0;
int inflateX = 0;
int inflateY = 0;
bool useCircleInflate = false;
float worldRadius = 0f;
Vector2 worldOffset = Vector2.zero;  // 记录 BoxCollider2D 的世界偏移

if (bossCollider != null)
{
    CircleCollider2D circle = bossCollider as CircleCollider2D;
    if (circle != null)
    {
        // 1) 计算 CircleCollider2D 的世界半径
        float maxScale = Mathf.Max(circle.transform.lossyScale.x, circle.transform.lossyScale.y);
        worldRadius = circle.radius * maxScale;
        // 2) 圆形膨胀：向外 +1 个节点
        inflateRadiusInNodes = Mathf.CeilToInt(worldRadius / nodeSize) + 1;
        useCircleInflate = true;
        Debug.Log($"[A*] 使用 CircleCollider2D，世界半径 = {worldRadius}, 膨胀网格半径 = {inflateRadiusInNodes} (＋1)");

        // 3) 记录圆心的世界偏移 = transform.position + offset * lossyScale
        worldOffset = new Vector2(
            circle.offset.x * circle.transform.lossyScale.x,
            circle.offset.y * circle.transform.lossyScale.y
        );
    }
    else
    {
        BoxCollider2D box = bossCollider as BoxCollider2D;
        if (box != null)
        {
            // 1) BoxCollider2D 在世界中的半宽半高
            Vector3 lossy = box.transform.lossyScale;
            float halfW = box.size.x * 0.5f * lossy.x;
            float halfH = box.size.y * 0.5f * lossy.y;
            // 2) 矩形膨胀：向外 +1 个节点
            inflateX = Mathf.CeilToInt(halfW / nodeSize) + 1;
            inflateY = Mathf.CeilToInt(halfH / nodeSize) + 1;
            Debug.Log($"[A*] 使用 BoxCollider2D，世界半宽 = {halfW}, 世界半高 = {halfH}, 膨胀网格 X = {inflateX}(＋1), Y = {inflateY}(＋1)");

            // 3) 记录世界偏移（要把 box.offset * lossyScale 加上去）
            worldOffset = new Vector2(
                box.offset.x * lossy.x,
                box.offset.y * lossy.y
            );
        }
        else
        {
            // 其它 Collider2D 类型：fallback 用 bounds.extents 做矩形膨胀
            Bounds ext = bossCollider.bounds;
            float halfW = ext.extents.x;
            float halfH = ext.extents.y;
            inflateX = Mathf.CeilToInt(halfW / nodeSize) + 1;
            inflateY = Mathf.CeilToInt(halfH / nodeSize) + 1;
            Debug.LogWarning($"[A*] 未识别的 Collider2D 类型，使用 bounds.extents 进行矩形膨胀，半宽 = {halfW}, 半高 = {halfH}, 膨胀网格 X = {inflateX}(＋1), Y = {inflateY}(＋1)");

            // worldOffset = (0,0)，因为我们不知道具体 offset，这里就不额外偏移
            worldOffset = Vector2.zero;
        }
    }
}

// 4. 对每个原始障碍做膨胀，把附近所有在碰撞体范围（+1）内的格子标为不可行走
foreach (Vector2Int blocked in rawBlockedCells)
{
    int bx = blocked.x;
    int by = blocked.y;

    // 1) 先算出“原始瓦片中心点”在世界空间的位置：
    Vector3 obstacleCenter = GridToWorld(bx, by);
    // 2) 再把 Collider2D 的 worldOffset 加上去，得到“膨胀参考中心”：
    Vector3 inflateCenter = obstacleCenter + new Vector3(worldOffset.x, worldOffset.y, 0f);

    if (bossCollider == null)
    {
        // 如果没有 collider，就一格一格地标记
        walkable[bx, by] = false;
        continue;
    }

    if (useCircleInflate)
    {
        // 圆形膨胀：在一个正方形区域里遍历，然后按照距离 inﬂateCenter 判断
        for (int dx = -inflateRadiusInNodes; dx <= inflateRadiusInNodes; dx++)
        {
            for (int dy = -inflateRadiusInNodes; dy <= inflateRadiusInNodes; dy++)
            {
                int nx = bx + dx;
                int ny = by + dy;
                if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                    continue;

                // 先算出这个网格中心的世界坐标
                Vector3 thisNodeCenter = GridToWorld(nx, ny);
                float dist = Vector3.Distance(thisNodeCenter, inflateCenter);

                // 原半径 + 半格 (nodeSize*0.5f) 再 +1 格（由于 Ceil + 1 的缘故，这里给一点缓冲）
                if (dist <= worldRadius + nodeSize * 0.5f)
                {
                    walkable[nx, ny] = false;
                }
            }
        }
    }
    else
    {
        // 矩形膨胀（BoxCollider2D 或 其它 Collider2D）
        // 这里我们要保证“正方形范围”是以 inflateCenter 对齐，而不是以 “GridToWorld(bx,by)” 对齐。
        // 先算出 inflateCenter 属于哪个格子：
        if (!WorldToGrid(inflateCenter, out int centerGX, out int centerGY))
        {
            // 如果 inflateCenter 本身已经溢出网格范围，就直接把原来的障碍点标一下
            walkable[bx, by] = false;
        }
        else
        {
            // inflateX, inflateY 分别是半宽/半高对应的格子数
            for (int dx = -inflateX; dx <= inflateX; dx++)
            {
                for (int dy = -inflateY; dy <= inflateY; dy++)
                {
                    int nx = centerGX + dx;
                    int ny = centerGY + dy;
                    if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                        continue;
                    walkable[nx, ny] = false;
                }
            }
        }
    }
}

        // 4. 对每个原始障碍做膨胀，把附近所有在碰撞体范围（+1）内的格子标为不可行走
        foreach (Vector2Int blocked in rawBlockedCells)
        {
            int bx = blocked.x;
            int by = blocked.y;

            if (bossCollider == null)
            {
                // 不做膨胀，直接标记障碍中心格子
                walkable[bx, by] = false;
                continue;
            }

            if (useCircleInflate)
            {
                // 圆形膨胀：在正方形范围内遍历，然后用距离判断
                for (int dx = -inflateRadiusInNodes; dx <= inflateRadiusInNodes; dx++)
                {
                    for (int dy = -inflateRadiusInNodes; dy <= inflateRadiusInNodes; dy++)
                    {
                        int nx = bx + dx;
                        int ny = by + dy;

                        if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                            continue;

                        // 计算网格中心到原始障碍中心的世界距离
                        Vector3 thisNodeCenter = GridToWorld(nx, ny);
                        Vector3 originalCenter = GridToWorld(bx, by);
                        float dist = Vector3.Distance(thisNodeCenter, originalCenter);

                        // 在原来 radius 范围内都算，再多保留 1 格
                        if (dist <= worldRadius + nodeSize * 0.5f)
                        {
                            walkable[nx, ny] = false;
                        }
                    }
                }
            }
            else
            {
                // 矩形膨胀（BoxCollider2D 或 其它 Collider2D）
                for (int dx = -inflateX; dx <= inflateX; dx++)
                {
                    for (int dy = -inflateY; dy <= inflateY; dy++)
                    {
                        int nx = bx + dx;
                        int ny = by + dy;

                        if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                            continue;

                        walkable[nx, ny] = false;
                    }
                }
            }
        }


        // 5. 根据 walkable 数组来初始化 nodes
        int blockedCount = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                nodes[x, y] = new Node(x, y, walkable[x, y]);
                if (!walkable[x, y]) blockedCount++;
            }
        }
        Debug.Log($"[A*] BuildWalkableGrid: 完成构建，共标记 {blockedCount} 个障碍格子（含膨胀）");
    }

    /// <summary>
    /// 将世界坐标 worldPos 转换为网格索引 (gridX, gridY)。若超出范围返回 false。
    /// </summary>
    public bool WorldToGrid(Vector3 worldPos, out int gridX, out int gridY)
    {
        Vector3 local = worldPos - originPosition;
        gridX = Mathf.FloorToInt(local.x / nodeSize);
        gridY = Mathf.FloorToInt(local.y / nodeSize);
        if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
            return false;
        return true;
    }

    /// <summary>
    /// 把网格索引 (gridX, gridY) 转换为该格子中心的世界坐标
    /// </summary>
    public Vector3 GridToWorld(int gridX, int gridY)
    {
        return originPosition + new Vector3(
            (gridX + 0.5f) * nodeSize,
            (gridY + 0.5f) * nodeSize,
            0f
        );
    }

    /// <summary>
    /// A* 寻路接口：从 startWorld 到 endWorld，返回一系列“格子中心世界坐标”作为路径，找不到返回 null。
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
{
    Debug.Log($"[A*] 开始寻路：start={startWorld}, end={endWorld}");

    if (gridWidth <= 0 || gridHeight <= 0)
    {
        Debug.LogWarning("[A*] 网格宽高无效，直接返回 null");
        return null;
    }
    // 1. 把 世界坐标 -> 格子索引
    if (!WorldToGrid(startWorld, out int startX, out int startY))
    {
        Debug.LogWarning($"[A*] 起点 {startWorld} WorldToGrid 失败");
        return null;
    }
    if (!WorldToGrid(endWorld, out int endX, out int endY))
    {
        Debug.LogWarning($"[A*] 终点 {endWorld} WorldToGrid 失败");
        return null;
    }

    // 2. 如果“终点格子”刚好被标记为障碍，就尝试往四周扩散找最近的 walkable
    if (!walkable[endX, endY])
    {
        Debug.LogWarning($"[A*] 终点 网格({endX},{endY}) 标记为障碍，不可行走，开始向外找最近可走点");
        if (!FindNearestWalkable(ref endX, ref endY))
        {
            Debug.LogWarning($"[A*] 未找到任何附近可走点，返回 null");
            return null;
        }
        Vector3 newEndWorld = GridToWorld(endX, endY);
        Debug.Log($"[A*] 已将终点 “{endWorld}” 调整到最近可走格子 ({endX},{endY}) -> 世界坐标 {newEndWorld}");
        endWorld = newEndWorld;
    }

    Node startNode = nodes[startX, startY];
    Node endNode   = nodes[endX, endY];

    if (!startNode.walkable)
        Debug.LogWarning($"[A*] 起点 网格({startX},{startY}) 标记为障碍，不可行走");
    if (!endNode.walkable)
        Debug.LogWarning($"[A*] 调整后终点 网格({endX},{endY}) 仍标记为障碍，不可行走");

    // 下面是原有的 A* 核心逻辑……
    List<Node> openList = new List<Node>();
    HashSet<Node> closedSet = new HashSet<Node>();
    // Reset
    for (int x = 0; x < gridWidth; x++)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            nodes[x, y].gCost = int.MaxValue;
            nodes[x, y].hCost = 0;
            nodes[x, y].parent = null;
        }
    }
    startNode.gCost = 0;
    startNode.hCost = GetDistance(startNode, endNode);
    openList.Add(startNode);

    while (openList.Count > 0)
    {
        Node current = openList[0];
        for (int i = 1; i < openList.Count; i++)
        {
            if (openList[i].fCost < current.fCost ||
                (openList[i].fCost == current.fCost && openList[i].hCost < current.hCost))
            {
                current = openList[i];
            }
        }
        openList.Remove(current);
        closedSet.Add(current);

        if (current == endNode)
        {
            Debug.Log($"[A*] 找到路径，开始回溯");
            return RetracePath(startNode, endNode);
        }

        foreach (Node neighbor in GetNeighbors(current))
        {
            if (!neighbor.walkable || closedSet.Contains(neighbor))
                continue;

            int newG = current.gCost + GetDistance(current, neighbor);
            if (newG < neighbor.gCost)
            {
                neighbor.gCost = newG;
                neighbor.hCost = GetDistance(neighbor, endNode);
                neighbor.parent = current;

                if (!openList.Contains(neighbor))
                    openList.Add(neighbor);
            }
        }
    }

    Debug.LogWarning("[A*] OpenList 为空，未找到可行路径，返回 null");
    return null;
}

/// <summary>
/// 如果 (gridX,gridY) 不是 walkable，就从它开始做 BFS/扩散，找到「最近的 walkable」格子。
/// 调用前确保 gridX,gridY 在 [0,gridWidth)×[0,gridHeight) 范围内。
/// 如果找到了，把 gridX,gridY 更新成新的、最近的那个 walkable，并返回 true；找不到则返回 false。
/// </summary>
private bool FindNearestWalkable(ref int gridX, ref int gridY)
{
    if (walkable[gridX, gridY]) return true;

    // 四个方向扩散步长集合 (Manhattan 距离)
    Queue<Vector2Int> queue = new Queue<Vector2Int>();
    bool[,] visited = new bool[gridWidth, gridHeight];
    queue.Enqueue(new Vector2Int(gridX, gridY));
    visited[gridX, gridY] = true;

    int[,] dirs = new int[,] { { 1,0 },{ -1,0 },{ 0,1 },{ 0,-1 } };
    while (queue.Count > 0)
    {
        Vector2Int cur = queue.Dequeue();
        for (int i = 0; i < 4; i++)
        {
            int nx = cur.x + dirs[i, 0];
            int ny = cur.y + dirs[i, 1];
            if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight) 
                continue;
            if (visited[nx, ny]) 
                continue;

            // 一旦找到一个 walkable = true 就直接返回
            if (walkable[nx, ny])
            {
                gridX = nx;
                gridY = ny;
                return true;
            }
            visited[nx, ny] = true;
            queue.Enqueue(new Vector2Int(nx, ny));
        }
    }

    return false;
}

    /// <summary>
    /// 回溯起点到终点的节点链表并转换为世界坐标列表
    /// </summary>
    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node current = endNode;
        while (current != startNode)
        {
            path.Add(GridToWorld(current.x, current.y));
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 获取当前节点的 4 个方向邻居
    /// </summary>
    private List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        int[,] dirs = new int[,] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
        for (int i = 0; i < dirs.GetLength(0); i++)
        {
            int nx = node.x + dirs[i, 0];
            int ny = node.y + dirs[i, 1];
            if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
            {
                neighbors.Add(nodes[nx, ny]);
            }
        }
        return neighbors;
    }

    /// <summary>
    /// 计算两个节点之间的曼哈顿距离
    /// </summary>
    private int GetDistance(Node a, Node b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
